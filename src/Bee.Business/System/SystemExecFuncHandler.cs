using Bee.Definition.Settings;
using Bee.Definition.Collections;
using Bee.Business.Attributes;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// Custom method handler for system-level business logic objects.
    /// </summary>
    internal class SystemExecFuncHandler : IExecFuncHandler
    {
        private readonly IRepositoryFactory _repositoryFactory;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemExecFuncHandler"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="repositoryFactory">Factory that builds framework repositories on demand.</param>
        public SystemExecFuncHandler(Guid accessToken, IRepositoryFactory repositoryFactory)
        {
            AccessToken = accessToken;
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
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
        /// <remarks>
        /// WARNING: local calls only. The caller names the target `DatabaseId`, and an upgrade that
        /// cannot be expressed as an ALTER falls back to rebuilding the table — create a temporary
        /// copy, drop the original, rename. Authentication alone is not an adequate gate for a
        /// destructive operation against a caller-chosen database.
        /// </remarks>
        [ExecFuncAccessControl(ApiAccessRequirement.Authenticated, LocalOnly = true)]
        public void UpgradeTableSchema(ExecFuncArgs args, ExecFuncResult result)
        {
            string databaseId = args.Parameters.GetValue<string>("DatabaseId");
            string categoryId = args.Parameters.GetValue<string>("CategoryId");
            string tableName = args.Parameters.GetValue<string>("TableName");

            var repo = _repositoryFactory.Create<IDatabaseRepository>();
            bool upgraded = repo.UpgradeTableSchema(databaseId, categoryId, tableName);
            result.Parameters.Add("Upgraded", upgraded);
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        /// <remarks>
        /// WARNING: local calls only. The supplied `DatabaseItem` can carry a complete connection
        /// string, so a remote caller would be able to make the server open an outbound connection to
        /// any host and port it names, and to substitute values into the server's own connection
        /// string. Both are server-side reach that authentication does not constrain.
        /// </remarks>
        [ExecFuncAccessControl(ApiAccessRequirement.Authenticated, LocalOnly = true)]
        public void TestConnection(ExecFuncArgs args, ExecFuncResult result)
        {
            var item = args.Parameters.GetValue<DatabaseItem>("DatabaseItem");

            var repo = _repositoryFactory.Create<IDatabaseRepository>();
            repo.TestConnection(item);
        }

    }
}
