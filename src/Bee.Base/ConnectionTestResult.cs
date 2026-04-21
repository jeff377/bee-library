namespace Bee.Base
{
    /// <summary>
    /// Represents the result of a connection test.
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConnectionTestResult"/>.
        /// </summary>
        public ConnectionTestResult()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ConnectionTestResult"/>.
        /// </summary>
        /// <param name="isSuccess">Whether the connection test succeeded.</param>
        /// <param name="message">The error or status message.</param>
        public ConnectionTestResult(bool isSuccess, string message)
        {
            this.IsSuccess = isSuccess;
            this.Message = message;;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the connection test succeeded.
        /// </summary>
        public bool IsSuccess { get; set; } = false;

        /// <summary>
        /// Gets or sets the error or status message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
