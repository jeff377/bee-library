using Bee.Definition.Identity;

namespace Bee.Northwind.Server;

/// <summary>
/// <see cref="ICompanyInfoService"/> that returns a single hard-coded demo company without
/// touching the <c>st_company</c> table — the company-context analogue of
/// <see cref="NorthwindAuthenticatingSystemBusinessObject"/>'s hard-coded login.
/// </summary>
/// <remarks>
/// It exists so company-scoped forms (<c>CategoryId="company"</c>) resolve their database:
/// the router maps <c>SessionInfo.CompanyId</c> → this service → <c>CompanyDatabaseId</c>.
/// <see cref="Set"/> / <see cref="Remove"/> are no-ops because the demo company is fixed.
/// </remarks>
public sealed class NorthwindCompanyInfoService : ICompanyInfoService
{
    private static readonly CompanyInfo s_company = new()
    {
        CompanyId = NorthwindCredentials.CompanyId,
        CompanyName = NorthwindCredentials.CompanyName,
        CompanyDatabaseId = NorthwindCredentials.CompanyDatabaseId,
    };

    /// <inheritdoc/>
    public CompanyInfo? Get(string companyId)
        => string.Equals(companyId, s_company.CompanyId, StringComparison.OrdinalIgnoreCase)
            ? s_company
            : null;

    /// <inheritdoc/>
    public void Set(CompanyInfo companyInfo) { /* Fixed demo company; nothing to persist. */ }

    /// <inheritdoc/>
    public void Remove(string companyId) { /* Fixed demo company; nothing to evict. */ }
}
