using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Bee.Base.Serialization;

namespace Bee.Base
{
    /// <summary>
    /// Represents an exception error that occurred during an API method call.
    /// </summary>
    [Serializable]
    [SuppressMessage("Minor Code Smell", "S2166:Classes named like \"Exception\" should extend \"Exception\" or a subclass",
        Justification = "ApiException is a serializable DTO carrying API error info across the JSON-RPC boundary, not a thrown exception. Renaming would break the published 4.x public API surface.")]
    public class ApiException : IObjectSerializeBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ApiException"/>.
        /// </summary>
        public ApiException()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ApiException"/>.
        /// </summary>
        /// <param name="exception">The exception that occurred at runtime.</param>
        /// <param name="includeStackTrace">
        /// When <c>true</c>, the stack trace is populated. Should only be set in debug/development environments
        /// to avoid leaking server internals to clients.
        /// </param>
        public ApiException(Exception exception, bool includeStackTrace = false)
        {
            Message = exception.Message;
            StackTrace = includeStackTrace ? (exception.StackTrace ?? string.Empty) : string.Empty;
        }

        #endregion

        /// <summary>
        /// Gets or sets the exception error message.
        /// </summary>
        [DefaultValue("")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call stack trace.
        /// </summary>
        [DefaultValue("")]
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the exception has been handled.
        /// </summary>
        [Description("Gets or sets a value indicating whether the exception has been handled.")]
        [DefaultValue(false)]
        public bool IsHandle { get; set; } = false;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return this.Message;
        }
    }
}
