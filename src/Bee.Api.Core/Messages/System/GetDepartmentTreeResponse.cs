using Bee.Api.Contracts;
using Bee.Definition.Organization;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get department tree operation. The tree is a typed object,
    /// serialised as JSON on the Plain wire format and MessagePack otherwise.
    /// </summary>
    [MessagePackObject]
    public class GetDepartmentTreeResponse : ApiResponse, IGetDepartmentTreeResponse
    {
        /// <summary>
        /// Gets or sets the current company's department tree (<c>null</c> when no company is entered).
        /// </summary>
        [Key(100)]
        public DepartmentTree? Tree { get; set; }
    }
}
