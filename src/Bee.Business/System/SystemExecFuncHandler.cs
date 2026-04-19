using Bee.Definition.Settings;
using Bee.Business.Attributes;
using Bee.Definition;
using Bee.Repository.Abstractions;

namespace Bee.Business.System
{
    /// <summary>
    /// Custom method handler for system-level business logic objects.
    /// </summary>
    internal class SystemExecFuncHandler : IExecFuncHandler
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemExecFuncHandler"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SystemExecFuncHandler(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        #endregion

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// A hello test method.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        [ExecFuncAccessControl(ApiAccessRequirement.Anonymous)]
        public static void Hello(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Hello", "Hello system-level BusinessObject");
        }

        /// <summary>
        /// Upgrades the table schema for the specified table.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        public static void UpgradeTableSchema(ExecFuncArgs args, ExecFuncResult result)
        {
            string databaseId = args.Parameters.GetValue<string>("DatabaseId");
            string dbName = args.Parameters.GetValue<string>("DbName");
            string tableName = args.Parameters.GetValue<string>("TableName");

            var repo = RepositoryInfo.SystemProvider!.DatabaseRepository;
            bool upgraded = repo.UpgradeTableSchema(databaseId, dbName, tableName);
            result.Parameters.Add("Upgraded", upgraded);
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        public static void TestConnection(ExecFuncArgs args, ExecFuncResult result)
        {
            var item = args.Parameters.GetValue<DatabaseItem>("DatabaseItem");

            var repo = RepositoryInfo.SystemProvider!.DatabaseRepository;
            repo.TestConnection(item);
        }

    }
}
