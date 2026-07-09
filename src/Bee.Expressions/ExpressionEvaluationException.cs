namespace Bee.Expressions
{
    /// <summary>
    /// Thrown when an expression cannot be parsed or compiled — for example a syntax error or a
    /// reference to an identifier outside the evaluation sandbox. This signals a misconfigured
    /// expression definition, not an end-user data error.
    /// </summary>
    public class ExpressionEvaluationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ExpressionEvaluationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ExpressionEvaluationException(string message) : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ExpressionEvaluationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The underlying parse/compile exception.</param>
        public ExpressionEvaluationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Gets the expression text that failed, when available.
        /// </summary>
        public string? Expression { get; init; }
    }
}
