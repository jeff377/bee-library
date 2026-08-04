using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Definition.Organization;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Reads a company's employee table (<c>st_employee</c>, a company-database table). Resolves the
    /// employee linked to a user (<c>user_rowid</c>) so the user's department can be derived for
    /// record-scope filtering.
    /// </summary>
    public class EmployeeRepository : RepositoryBase, IEmployeeRepository
    {
        /// <summary>
        /// Initializes a new <see cref="EmployeeRepository"/>.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        /// <remarks>
        /// Declares no scope: its tables live in a company database, but its callers are cache
        /// providers and session bootstrap, which are told which company to read and hold no token
        /// to route with. Routing by session here would read the caller's company instead of the
        /// requested one.
        /// </remarks>
        public EmployeeRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, null)
        {
        }

        /// <inheritdoc/>
        public EmployeeRow? GetByUserRowId(string databaseId, Guid userRowId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            if (userRowId == Guid.Empty) { return null; }

            var dbType = Context.ConnectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_employee");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDept = dbType.QuoteIdentifier("dept_rowid");
            string colUser = dbType.QuoteIdentifier("user_rowid");

            string sql = $"SELECT {colRowId}, {colId}, {colName}, {colDept}, {colUser} FROM {tbl} WHERE {colUser} = {{0}}";
            var dbAccess = CreateDbAccess(databaseId);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql, userRowId)).Table!;
            if (table.Rows.Count == 0) { return null; }

            DataRow row = table.Rows[0];
            return new EmployeeRow(
                ValueUtilities.CGuid(row["sys_rowid"]),
                ValueUtilities.CStr(row["sys_id"]),
                ValueUtilities.CStr(row["sys_name"]),
                ValueUtilities.CGuid(row["dept_rowid"]),
                ValueUtilities.CGuid(row["user_rowid"]));
        }
    }
}
