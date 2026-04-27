using System.ComponentModel;
using Bee.Base.Serialization;

namespace Bee.Api.Core.Messages
{
    /// <summary>
    /// Carries error information for an API method call across the JSON-RPC boundary.
    /// This is a serializable DTO, not a thrown exception.
    /// </summary>
    [Serializable]
    public class ApiErrorInfo : IObjectSerializeBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ApiErrorInfo"/>.
        /// </summary>
        public ApiErrorInfo()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ApiErrorInfo"/> from a runtime exception.
        /// </summary>
        /// <param name="exception">The exception that occurred at runtime.</param>
        /// <param name="includeStackTrace">
        /// When <c>true</c>, the stack trace is populated. Should only be set in debug/development environments
        /// to avoid leaking server internals to clients.
        /// </param>
        public ApiErrorInfo(Exception exception, bool includeStackTrace = false)
        {
            Message = exception.Message;
            StackTrace = includeStackTrace ? (exception.StackTrace ?? string.Empty) : string.Empty;
        }

        #endregion

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [DefaultValue("")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call stack trace.
        /// </summary>
        [DefaultValue("")]
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the error has been handled.
        /// </summary>
        [Description("Gets or sets a value indicating whether the error has been handled.")]
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
