using System.Collections.Concurrent;
using System.Text;
using DynamicExpresso;
using DynamicExpresso.Exceptions;

namespace Bee.Expressions
{
    /// <summary>
    /// An <see cref="IExpressionEvaluator"/> backed by DynamicExpresso. Expressions are parsed and
    /// compiled once, cached by their text and parameter signature, then invoked per row.
    /// </summary>
    /// <remarks>
    /// The evaluation sandbox exposes only the supplied variables, primitive types, a small set of
    /// common types (such as <see cref="Math"/>), and the helper functions registered in the
    /// constructor. Any other identifier — reflection, file or network access, arbitrary type
    /// loading — is an unknown identifier and fails at parse time, so a definition-authored
    /// expression cannot reach outside this surface.
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
        /// Registers the curated helper functions available to every expression.
        /// </summary>
        private static void RegisterHelperFunctions(Interpreter interpreter)
        {
            // Expose Guid so expressions can test key/reference fields (for example
            // `customer_rowid != Guid.Empty`). Guid is a value type with no IO surface.
            interpreter.Reference(typeof(Guid));

            interpreter.SetFunction("Today", (Func<DateTime>)(() => DateTime.Today));
            interpreter.SetFunction("Now", (Func<DateTime>)(() => DateTime.Now));
            interpreter.SetFunction("IsNullOrEmpty", (Func<string?, bool>)string.IsNullOrEmpty);
            interpreter.SetFunction("IsNullOrWhiteSpace", (Func<string?, bool>)string.IsNullOrWhiteSpace);
        }

        /// <inheritdoc />
        public object? Evaluate(string expression, IReadOnlyDictionary<string, object?> variables, Type returnType)
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

            return lambda.Invoke(arguments);
        }

        /// <inheritdoc />
        public T Evaluate<T>(string expression, IReadOnlyDictionary<string, object?> variables)
        {
            var result = Evaluate(expression, variables, typeof(T));
            return result is null ? default! : (T)result;
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
