using System.Collections.Concurrent;
using System.Text;
using DynamicExpresso;
using DynamicExpresso.Exceptions;
using Bee.Base;
using Bee.Base.Expressions;

namespace Bee.Expressions
{
    /// <summary>
    /// An <see cref="IExpressionEvaluator"/> backed by DynamicExpresso. Expressions are parsed and
    /// compiled once, cached by their text and parameter signature, then invoked per row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The evaluator exposes the supplied variables, primitive types, a small set of common types
    /// (such as <see cref="Math"/>), and the helper functions registered in the constructor. A
    /// <em>type name</em> that was not registered — <c>File</c>, <c>Assembly</c>, <c>Process</c> —
    /// is an unknown identifier and fails at parse time.
    /// </para>
    /// <para>
    /// WARNING: that is not the same as a security sandbox, and this class must not be treated as
    /// one. Member access on a value is resolved by reflection, and <c>GetType()</c> is a public
    /// member of <see cref="object"/>, so any variable in scope is a starting point for
    /// <c>someField.GetType().Assembly</c> and onward through the reflection API. Upstream
    /// DynamicExpresso states the same limitation: it is not built to evaluate untrusted input.
    /// </para>
    /// <para>
    /// What keeps this safe in practice is the source of the expressions, not the parser:
    /// expressions live in definition files, and writing a definition is a deployment-time
    /// operation (<c>SystemBO.SaveDefine</c> is <c>LocalOnly</c>). Anything that would let a remote
    /// or lower-privileged caller supply expression text turns this into remote code execution on
    /// the server, so that boundary is the control — keep it intact.
    /// </para>
    /// </remarks>
    public sealed class DynamicExpressoEvaluator : IExpressionEvaluator
    {
        private readonly Interpreter _interpreter;
        private readonly ConcurrentDictionary<string, Lambda> _cache = new(StringComparer.Ordinal);
        private readonly object _parseLock = new();

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicExpressoEvaluator"/>.
        /// </summary>
        public DynamicExpressoEvaluator()
        {
            // Default options register primitive and common types (Math, Convert, ...) but no
            // reflection, IO, or arbitrary type loading — those remain unknown identifiers.
            _interpreter = new Interpreter(InterpreterOptions.Default);
            RegisterHelperFunctions(_interpreter);
        }

        /// <summary>
        /// The time zone the helper functions read, for the duration of one <c>Evaluate</c> call.
        /// </summary>
        /// <remarks>
        /// DynamicExpresso binds a helper to a fixed delegate at registration, so a per-call zone has
        /// to reach the delegate through captured state. This field is that channel, and it is a
        /// confined implementation detail: the public contract takes the zone as an argument
        /// (ADR-032 D13), and this evaluator is a singleton whose helpers must therefore not hold one
        /// user's zone.
        ///
        /// <c>[ThreadStatic]</c> rather than <c>AsyncLocal</c> because the window is one synchronous
        /// <c>lambda.Invoke</c> — no await intervenes, so the value cannot leak to another logical
        /// call. Keeping the zone out of the cache key is the point: one compiled lambda serves every
        /// user regardless of zone.
        /// </remarks>
        [ThreadStatic]
        private static string? t_timeZoneId;

        /// <summary>
        /// Registers the curated helper functions available to every expression.
        /// </summary>
        /// <param name="interpreter">The interpreter to register into.</param>
        private static void RegisterHelperFunctions(Interpreter interpreter)
        {
            // Expose Guid so expressions can test key/reference fields (for example
            // `customer_rowid != Guid.Empty`). Guid is a value type with no IO surface.
            interpreter.Reference(typeof(Guid));

            // `Today()` yields a calendar day in the user's zone (ADR-032 D12). It is a DateOnly
            // like every other date in the framework; `ExpressionPolicy.CoerceValue` widens it when
            // the result lands in a DataSet cell, the one place a date must be a DateTime.
            interpreter.SetFunction("Today", (Func<DateOnly>)(() => FrameworkClock.Today(t_timeZoneId ?? string.Empty)));
            // `Now()` deliberately does no time-zone work beyond the same lookup: an instant written
            // into a DateTime cell is subject to the Connector's conversion, and expressions
            // computing on instants are rare enough that the author picks the semantics (D12).
            interpreter.SetFunction("Now", (Func<DateTime>)(() => FrameworkClock.Now(t_timeZoneId ?? string.Empty)));
            interpreter.SetFunction("UtcNow", (Func<DateTime>)(() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)));
            interpreter.SetFunction("IsNullOrEmpty", (Func<string?, bool>)string.IsNullOrEmpty);
            interpreter.SetFunction("IsNullOrWhiteSpace", (Func<string?, bool>)string.IsNullOrWhiteSpace);
        }

        /// <inheritdoc />
        public object? Evaluate(string expression, IReadOnlyDictionary<string, object?> variables, Type returnType,
            string timeZoneId = "")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expression);
            ArgumentNullException.ThrowIfNull(variables);
            ArgumentNullException.ThrowIfNull(returnType);

            // Stable parameter order so the cache key and the compiled signature are deterministic.
            var names = variables.Keys.ToArray();
            Array.Sort(names, StringComparer.Ordinal);

            var lambda = GetOrCompile(expression, returnType, names, variables);

            var arguments = new Parameter[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                arguments[i] = new Parameter(names[i], variables[names[i]] ?? (object)string.Empty);
            }

            return InvokeWithZone(lambda, arguments, timeZoneId);
        }

        /// <inheritdoc />
        public T Evaluate<T>(string expression, IReadOnlyDictionary<string, object?> variables, string timeZoneId = "")
        {
            var result = Evaluate(expression, variables, typeof(T), timeZoneId);
            return result is null ? default! : (T)result;
        }

        /// <summary>
        /// Invokes a compiled lambda with <see cref="t_timeZoneId"/> set to <paramref name="timeZoneId"/>.
        /// </summary>
        /// <param name="lambda">The compiled expression to invoke.</param>
        /// <param name="arguments">The bound parameters.</param>
        /// <param name="timeZoneId">The zone the helper functions should observe for this invocation.</param>
        /// <remarks>
        /// The zone is scoped to this invocation only, and whatever an outer call had is restored: a
        /// computed field's expression can be evaluated inside another evaluation. Kept static so the
        /// whole read/write protocol for the ambient field sits next to the field itself.
        /// </remarks>
        private static object? InvokeWithZone(Lambda lambda, Parameter[] arguments, string timeZoneId)
        {
            var previous = t_timeZoneId;
            t_timeZoneId = timeZoneId;
            try
            {
                return lambda.Invoke(arguments);
            }
            finally
            {
                t_timeZoneId = previous;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetReferencedVariables(string expression)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expression);
            try
            {
                lock (_parseLock)
                {
                    return _interpreter.DetectIdentifiers(expression).UnknownIdentifiers.ToArray();
                }
            }
            catch (ParseException ex)
            {
                throw new ExpressionEvaluationException(
                    $"Failed to parse expression: {ex.Message}", ex) { Expression = expression };
            }
        }

        /// <summary>
        /// Returns the cached compiled lambda for the expression/return-type/parameter signature,
        /// compiling and caching it on first use.
        /// </summary>
        private Lambda GetOrCompile(string expression, Type returnType, string[] names,
            IReadOnlyDictionary<string, object?> variables)
        {
            var key = BuildCacheKey(expression, returnType, names, variables);
            return _cache.GetOrAdd(key, _ =>
            {
                var parameters = new Parameter[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    var type = variables[names[i]]?.GetType() ?? typeof(object);
                    parameters[i] = new Parameter(names[i], type);
                }
                try
                {
                    lock (_parseLock)
                    {
                        return _interpreter.Parse(expression, returnType, parameters);
                    }
                }
                catch (ParseException ex)
                {
                    throw new ExpressionEvaluationException(
                        $"Failed to parse expression: {ex.Message}", ex) { Expression = expression };
                }
            });
        }

        /// <summary>
        /// Builds a deterministic cache key from the expression, its return type, and the ordered
        /// parameter name/type signature.
        /// </summary>
        private static string BuildCacheKey(string expression, Type returnType, string[] names,
            IReadOnlyDictionary<string, object?> variables)
        {
            var builder = new StringBuilder(returnType.FullName)
                .Append('|').Append(expression).Append('|');
            foreach (var name in names)
            {
                var type = variables[name]?.GetType() ?? typeof(object);
                builder.Append(name).Append(':').Append(type.FullName).Append(',');
            }
            return builder.ToString();
        }
    }
}
