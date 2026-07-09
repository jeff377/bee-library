namespace Bee.Expressions
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
        /// <exception cref="ExpressionEvaluationException">The expression cannot be parsed or references
        /// an identifier outside the evaluation sandbox.</exception>
        object? Evaluate(string expression, IReadOnlyDictionary<string, object?> variables, Type returnType);

        /// <summary>
        /// Evaluates <paramref name="expression"/> and returns the result as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="expression">The expression text.</param>
        /// <param name="variables">The variable name/value pairs available to the expression.</param>
        /// <exception cref="ExpressionEvaluationException">The expression cannot be parsed or references
        /// an identifier outside the evaluation sandbox.</exception>
        T Evaluate<T>(string expression, IReadOnlyDictionary<string, object?> variables);

        /// <summary>
        /// Returns the names of the variables (unknown identifiers) that <paramref name="expression"/>
        /// references. Used to build the "which field changes force a recompute" dependency graph.
        /// </summary>
        /// <param name="expression">The expression text.</param>
        /// <exception cref="ExpressionEvaluationException">The expression cannot be parsed.</exception>
        IReadOnlyList<string> GetReferencedVariables(string expression);
    }
}
