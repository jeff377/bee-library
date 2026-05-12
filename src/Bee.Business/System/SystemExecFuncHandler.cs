using Bee.Definition.Settings;
using Bee.Business.Attributes;
using Bee.Repository.Abstractions.Factories;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// Custom method handler for system-level business logic objects.
    /// </summary>
    internal class SystemExecFuncHandler : IExecFuncHandler
    {
        private readonly ISystemRepositoryFactory _systemFactory;

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemExecFuncHandler"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="systemFactory">Factory that builds system-level repositories on demand.</param>
        public SystemExecFuncHandler(Guid accessToken, ISystemRepositoryFactory systemFactory)
        {
            AccessToken = accessToken;
            _systemFactory = systemFactory ?? throw new ArgumentNullException(nameof(systemFactory));
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
        public void UpgradeTableSchema(ExecFuncArgs args, ExecFuncResult result)
        {
            string databaseId = args.Parameters.GetValue<string>("DatabaseId");
            string categoryId = args.Parameters.GetValue<string>("CategoryId");
            string tableName = args.Parameters.GetValue<string>("TableName");

            var repo = _systemFactory.CreateDatabaseRepository();
            bool upgraded = repo.UpgradeTableSchema(databaseId, categoryId, tableName);
            result.Parameters.Add("Upgraded", upgraded);
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        public void TestConnection(ExecFuncArgs args, ExecFuncResult result)
        {
            var item = args.Parameters.GetValue<DatabaseItem>("DatabaseItem");

            var repo = _systemFactory.CreateDatabaseRepository();
            repo.TestConnection(item);
        }

    }
}
