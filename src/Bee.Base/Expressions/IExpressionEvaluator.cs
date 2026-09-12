using System.Diagnostics.CodeAnalysis;

namespace Bee.Base.Expressions
{
    /// <summary>
    /// Evaluates expressions (a C# expression subset) against a set of named variables. The engine
    /// is portable across the business layer and UI clients so that a field computed on the client
    /// for live preview yields the same result the server writes on save.
    /// </summary>
    /// <remarks>
    /// Implementations compile each expression once and cache the compiled delegate, then invoke it
    /// per row. Variables are supplied as name/value pairs; callers are expected to pass values
    /// already coerced to their field's CLR type (see <see cref="ExpressionPolicy"/>), so that a
    /// variable's type is stable across invocations and never <c>null</c>.
    /// </remarks>
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// Evaluates <paramref name="expression"/> and converts the result to
        /// <paramref name="returnType"/>.
        /// </summary>
        /// <param name="expression">The expression text.</param>
        /// <param name="variables">The variable name/value pairs available to the expression.</param>
        /// <param name="returnType">The expected result type (for example <see cref="bool"/> for a
        /// condition or <see cref="decimal"/> for a computed amount).</param>
        /// <param name="timeZoneId">
        /// The user's IANA time zone id, seen by the <c>Today()</c> helper, and by <c>Now()</c> when
        /// <paramref name="basis"/> is <see cref="DateTimeBasis.UserZone"/>. Blank means UTC.
        /// </param>
        /// <param name="basis">
        /// The basis of the data set the expression reads and writes, seen by the <c>Now()</c> helper so
        /// that its value shares the basis of the surrounding cells: <see cref="DateTimeBasis.Utc"/> for a
        /// server-side pass, <see cref="DateTimeBasis.UserZone"/> for a client-side preview (ADR-032 D12).
        /// </param>
        /// <exception cref="ExpressionEvaluationException">The expression cannot be parsed or references
        /// an identifier outside the evaluation sandbox.</exception>
        /// <remarks>
        /// The zone and the basis are per-call arguments, not evaluator state: an implementation is
        /// typically registered as a singleton and serves every user, so a zone fixed at construction
        /// could only ever be one user's (ADR-032 D13).
        /// </remarks>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters",
            Justification = "The overload pair is not new API: both members shipped together from Bee.Expressions and only changed namespace here (ADR-038). RS0026 guards against a caller silently rebinding when an optional-parameter overload is added alongside an existing one, which cannot happen when neither overload has ever existed without the other.")]
        object? Evaluate(string expression, IReadOnlyDictionary<string, object?> variables, Type returnType,
            string timeZoneId = "", DateTimeBasis basis = DateTimeBasis.UserZone);

        /// <summary>
        /// Evaluates <paramref name="expression"/> and returns the result as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="expression">The expression text.</param>
        /// <param name="variables">The variable name/value pairs available to the expression.</param>
        /// <param name="timeZoneId">The user's IANA time zone id; blank means UTC.</param>
        /// <param name="basis">The basis of the data set the expression reads and writes; see the non-generic overload.</param>
        /// <exception cref="ExpressionEvaluationException">The expression cannot be parsed or references
        /// an identifier outside the evaluation sandbox.</exception>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters",
            Justification = "See the justification on the non-generic overload above.")]
        T Evaluate<T>(string expression, IReadOnlyDictionary<string, object?> variables, string timeZoneId = "",
            DateTimeBasis basis = DateTimeBasis.UserZone);

        /// <summary>
        /// Returns the names of the variables (unknown identifiers) that <paramref name="expression"/>
        /// references. Used to build the "which field changes force a recompute" dependency graph.
        /// </summary>
        /// <param name="expression">The expression text.</param>
        /// <exception cref="ExpressionEvaluationException">The expression cannot be parsed.</exception>
        IReadOnlyList<string> GetReferencedVariables(string expression);
    }
}
