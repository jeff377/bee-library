using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the EnterCompany operation.
    /// </summary>
    [MessagePackObject]
    public class EnterCompanyRequest : ApiRequest, IEnterCompanyRequest
    {
        /// <summary>
        /// Gets or sets the id of the company the caller wants to enter for this session.
        /// </summary>
        [Key(100)]
        public string CompanyId { get; set; } = string.Empty;
    }
}
