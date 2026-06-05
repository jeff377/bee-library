using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Organization;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Reads a company's department nodes from <c>ft_department</c> (a company-database table).
    /// Every method takes the company database id explicitly; node relations use row ids
    /// (<c>sys_rowid</c> / <c>parent_rowid</c>), which the in-memory <see cref="DepartmentTree"/>
    /// turns into the hierarchy.
    /// </summary>
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="DepartmentRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public DepartmentRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public IReadOnlyList<DepartmentRow> GetDepartments(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbType = _connectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("ft_department");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colParent = dbType.QuoteIdentifier("parent_rowid");
            string colManager = dbType.QuoteIdentifier("manager_rowid");

            string sql = $"SELECT {colRowId}, {colId}, {colName}, {colParent}, {colManager} FROM {tbl}";
            var dbAccess = new DbAccess(databaseId, _connectionManager);
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
