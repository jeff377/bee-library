using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Definition.Organization;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Reads a company's department nodes from <c>st_department</c> (a company-database table).
    /// Every method takes the company database id explicitly; node relations use row ids
    /// (<c>sys_rowid</c> / <c>parent_rowid</c>), which the in-memory <see cref="DepartmentTree"/>
    /// turns into the hierarchy.
    /// </summary>
    public class DepartmentRepository : RepositoryBase, IDepartmentRepository
    {
        /// <summary>
        /// Initializes a new <see cref="DepartmentRepository"/>.
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
        public DepartmentRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, null)
        {
        }

        /// <inheritdoc/>
        public IReadOnlyList<DepartmentRow> GetDepartments(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = Context.ConnectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_department");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colParent = dbType.QuoteIdentifier("parent_rowid");
            string colManager = dbType.QuoteIdentifier("manager_rowid");

            string sql = $"SELECT {colRowId}, {colId}, {colName}, {colParent}, {colManager} FROM {tbl}";
            var dbAccess = CreateDbAccess(databaseId);
            var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable, sql)).Table!;

            var list = new List<DepartmentRow>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new DepartmentRow(
                    ValueUtilities.CGuid(row["sys_rowid"]),
                    ValueUtilities.CStr(row["sys_id"]),
                    ValueUtilities.CStr(row["sys_name"]),
                    ValueUtilities.CGuid(row["parent_rowid"]),
                    ValueUtilities.CGuid(row["manager_rowid"])));
            }
            return list;
        }
    }
}
