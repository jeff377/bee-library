using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Organization;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Reads a company's employee table (<c>st_employee</c>, a company-database table). Resolves the
    /// employee linked to a user (<c>user_rowid</c>) so the user's department can be derived for
    /// record-scope filtering.
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="EmployeeRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public EmployeeRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public EmployeeRow? GetByUserRowId(string databaseId, Guid userRowId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            if (userRowId == Guid.Empty) { return null; }

            var dbType = _connectionManager.GetConnectionInfo(databaseId).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_employee");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDept = dbType.QuoteIdentifier("dept_rowid");
            string colUser = dbType.QuoteIdentifier("user_rowid");

            string sql = $"SELECT {colRowId}, {colId}, {colName}, {colDept}, {colUser} FROM {tbl} WHERE {colUser} = {{0}}";
            var dbAccess = new DbAccess(databaseId, _connectionManager);
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
