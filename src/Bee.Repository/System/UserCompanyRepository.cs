using Bee.Base;
using Bee.Db;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Permission lookup for the <c>st_user_company</c> table — answers
    /// <see cref="HasAccess"/> via a three-table JOIN against <c>st_user</c> and
    /// <c>st_company</c>, filtered by the company's <c>enabled</c> flag.
    /// </summary>
    public class UserCompanyRepository : RepositoryBase, IUserCompanyRepository
    {
        /// <summary>
        /// Initializes a new <see cref="UserCompanyRepository"/>.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        public UserCompanyRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, DbScope.Common)
        {
        }

        /// <summary>
        /// Determines whether the user has access to the enabled company.
        /// </summary>
        /// <param name="userId">The user business id.</param>
        /// <param name="companyId">The company business id.</param>
        public bool HasAccess(string userId, string companyId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(companyId))
                return false;

            var dbType = Context.ConnectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string ucTbl = dbType.QuoteIdentifier("st_user_company");
            string userTbl = dbType.QuoteIdentifier("st_user");
            string companyTbl = dbType.QuoteIdentifier("st_company");
            string userRowId = dbType.QuoteIdentifier("user_rowid");
            string companyRowId = dbType.QuoteIdentifier("company_rowid");
            string sysRowId = dbType.QuoteIdentifier("sys_rowid");
            string sysId = dbType.QuoteIdentifier("sys_id");
            string enabled = dbType.QuoteIdentifier("enabled");

            string sql =
                $"SELECT COUNT(*) FROM {ucTbl} uc \n" +
                $"INNER JOIN {userTbl} u ON u.{sysRowId} = uc.{userRowId} \n" +
                $"INNER JOIN {companyTbl} c ON c.{sysRowId} = uc.{companyRowId} \n" +
                $"WHERE u.{sysId} = {{0}} AND c.{sysId} = {{1}} AND c.{enabled} = {{2}}";

            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, userId, companyId, true);
            var dbAccess = CreateDbAccess();
            var result = dbAccess.Execute(command);
            return ValueUtilities.CInt(result.Scalar!) > 0;
        }
    }
}
