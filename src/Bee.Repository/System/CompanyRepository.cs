using Bee.Base;
using Bee.Base.Data;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Data access object for company master records on the <c>st_company</c> table.
    /// </summary>
    /// <remarks>
    /// Disabled companies (<c>enabled = false</c>) are excluded at the query layer — to
    /// callers they look exactly like nonexistent companies, which matches the merged
    /// "Company access denied" error surface of <c>EnterCompany</c>.
    /// </remarks>
    public class CompanyRepository : ICompanyRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="CompanyRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public CompanyRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <summary>
        /// Gets the enabled company by its business id (<c>sys_id</c>); returns <c>null</c>
        /// when no matching enabled row exists.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        public CompanyInfo? GetById(string companyId)
        {
            var dbType = _connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_company");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDbId = dbType.QuoteIdentifier("company_database_id");
            string colEnabled = dbType.QuoteIdentifier("enabled");

            string sql = $"SELECT {colId}, {colName}, {colDbId} \n" +
                         $"FROM {tbl} \n" +
                         $"WHERE {colId} = {{0}} AND {colEnabled} = {{1}}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, companyId, true);
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var result = dbAccess.Execute(command);
            var table = result.Table!;
            if (table.IsEmpty()) { return null; }

            var row = table.Rows[0];
            return new CompanyInfo
            {
                CompanyId = ValueUtilities.CStr(row["sys_id"]),
                CompanyName = ValueUtilities.CStr(row["sys_name"]),
                CompanyDatabaseId = ValueUtilities.CStr(row["company_database_id"])
            };
        }
    }
}
