namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the Logout request.
    /// </summary>
    /// <remarks>
    /// Logout takes no parameters; the empty contract is kept for symmetry with Login
    /// and to leave room for future fields (e.g. a reason code).
    /// </remarks>
    public interface ILogoutRequest
    {
    }
}
