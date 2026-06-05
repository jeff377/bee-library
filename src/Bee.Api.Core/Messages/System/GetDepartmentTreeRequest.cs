using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get department tree operation. Carries no parameters — the tree is
    /// scoped to the caller's current company resolved from the session.
    /// </summary>
    [MessagePackObject]
    public class GetDepartmentTreeRequest : ApiRequest
    {
    }
}
