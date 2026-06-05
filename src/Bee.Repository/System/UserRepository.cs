using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Reads the common <c>st_user</c> table. Resolves a user's <c>sys_rowid</c> from its
    /// <c>sys_id</c> so company-scoped lookups (e.g. the employee link) can be keyed by row id.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public UserRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public Guid GetRowIdBySysId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) { return Guid.Empty; }

            var dbType = _connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");

            string sql = $"SELECT {colRowId} FROM {tbl} WHERE {colId} = {{0}}";
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar, sql, userId));
            // Scalar is null when the user id matches no row → no user, empty row id.
            return result.Scalar == null ? Guid.Empty : ValueUtilities.CGuid(result.Scalar);
        }
    }
}
