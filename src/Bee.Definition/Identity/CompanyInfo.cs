using Bee.Base;
using MessagePack;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// Metadata describing a company that a user may enter for a session.
    /// </summary>
    /// <remarks>
    /// Returned by <c>EnterCompany</c> and resolved by company-aware repositories
    /// via the cached <c>ICompanyInfoService</c>. The two database id properties
    /// reference logical <c>DatabaseSettings</c> entries; multiple companies may
    /// point at the same id and rely on the <c>sys_company_rowid</c> column for
    /// row-level isolation.
    /// </remarks>
    [MessagePackObject]
    public class CompanyInfo : IKeyObject
    {
        #region IKeyObject Interface

        /// <summary>
        /// Gets the item key value (the company id).
        /// </summary>
        public string GetKey()
        {
            return this.CompanyId;
        }

        #endregion

        /// <summary>
        /// Gets or sets the company id (unique key).
        /// </summary>
        [Key(0)]
        public string CompanyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company display name.
        /// </summary>
        [Key(1)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the logical <c>DatabaseSettings</c> id used for the
        /// company-category database during this session.
        /// </summary>
        [Key(2)]
        public string CompanyDatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the logical <c>DatabaseSettings</c> id used for the
        /// log-category database during this session.
        /// </summary>
        [Key(3)]
        public string LogDatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{CompanyId} : {CompanyName}";
        }
    }
}
