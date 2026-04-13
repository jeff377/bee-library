namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for create session request parameters.
    /// </summary>
    public interface ICreateSessionRequest
    {
        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        string UserID { get; }

        /// <summary>
        /// Gets the session expiration time in seconds.
        /// </summary>
        int ExpiresIn { get; }

        /// <summary>
        /// Gets a value indicating whether this is a one-time session.
        /// </summary>
        bool OneTime { get; }
    }
}
