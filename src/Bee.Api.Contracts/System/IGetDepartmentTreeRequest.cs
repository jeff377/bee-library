namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the get department tree request.
    /// </summary>
    /// <remarks>
    /// The request takes no parameters; the tree is scoped to the caller's current company
    /// resolved from the session. The empty contract is kept for symmetry with the other
    /// request types on this axis, so that every wire message has a matching contract.
    /// </remarks>
    public interface IGetDepartmentTreeRequest
    {
    }
}
