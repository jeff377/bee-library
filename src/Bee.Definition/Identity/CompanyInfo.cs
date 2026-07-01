using Bee.Base;
using MessagePack;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// Metadata describing a company that a user may enter for a session.
    /// </summary>
    /// <remarks>
    /// Returned by <c>EnterCompany</c> and resolved by company-aware repositories
    /// via the cached <c>ICompanyInfoService</c>. <c>CompanyDatabaseId</c> references
    /// a logical <c>DatabaseSettings</c> entry; multiple companies may point at the
    /// same id and rely on the <c>sys_company_rowid</c> column for row-level
    /// isolation. The log database is shared across all companies under a fixed
    /// <c>"log"</c> databaseId (see <c>DbScope.Log</c> in plan-bo-repo-db-routing),
    /// so there is no per-company log database id property.
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
        /// Gets or sets the tenant customization code for this company.
        /// </summary>
        /// <remarks>
        /// Empty means the standard (non-customized) deployment. Companies map many-to-one onto a
        /// customization code (a group can share one customization set). Loaded from the
        /// <c>customize_id</c> column by <c>CompanyRepository</c>; <c>EnterCompany</c> copies it
        /// into <c>SessionInfo.CustomizeId</c> for the session's customization overlay.
        /// </remarks>
        [Key(3)]
        public string CustomizeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company-level decimal-places override table. Empty means every kind uses
        /// the framework default. Loaded from the <c>number_formats_xml</c> column by
        /// <c>CompanyRepository</c>; carries Percent and UnitPrice/Cost display decimals plus the
        /// Quantity/Weight fallback when no unit is bound (see plan-numeric-core.md).
        /// </summary>
        [Key(4)]
        public CompanyNumberFormats NumberFormats { get; set; } = [];

        /// <summary>
        /// Gets the decimal places for the specified kind: the company override when present,
        /// otherwise the framework default from <see cref="NumberKindProfile.GetDefaultDecimals"/>.
        /// </summary>
        /// <param name="kind">The number kind.</param>
        public int GetDecimals(NumberKind kind)
        {
            return NumberFormats.FindDecimals(kind) ?? NumberKindProfile.GetDefaultDecimals(kind);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{CompanyId} : {CompanyName}";
        }
    }
}
