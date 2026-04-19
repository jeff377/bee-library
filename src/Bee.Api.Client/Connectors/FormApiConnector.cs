using Bee.Api.Core;
using Bee.Definition;

namespace Bee.Api.Client.Connectors
{
    /// <summary>
    /// Form-level API service connector.
    /// </summary>
    public class FormApiConnector : ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="FormApiConnector"/> class using a local connection.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        public FormApiConnector(Guid accessToken, string progId) : base(accessToken)
        {
            ProgId = progId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormApiConnector"/> class using a remote connection.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        public FormApiConnector(string endpoint, Guid accessToken, string progId) : base(endpoint, accessToken)
        {
            ProgId = progId;
        }

        #endregion

        /// <summary>
        /// Gets or sets the program identifier (ProgId) used to identify the form-level business object.
        /// </summary>
        public string ProgId { get; private set; }

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="action">The action name to execute.</param>
        /// <param name="value">The input parameter for the action.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        public async Task<T> ExecuteAsync<T>(string action, object value, PayloadFormat format = PayloadFormat.Encrypted)
        {
            return await base.ExecuteAsync<T>(ProgId, action, value, format).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a custom method; requires authentication.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public async Task<ExecFuncResponse> ExecFuncAsync(ExecFuncRequest args)
        {
            return await ExecuteAsync<ExecFuncResponse>(SystemActions.ExecFunc, args).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a custom method; requires authentication.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public ExecFuncResponse ExecFunc(ExecFuncRequest args)
        {
            return SyncExecutor.Run(() =>
                ExecFuncAsync(args)
            );
        }

        /// <summary>
        /// Asynchronously executes a custom method; allows anonymous access.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public async Task<ExecFuncResponse> ExecFuncAnonymousAsync(ExecFuncRequest args)
        {
            return await ExecuteAsync<ExecFuncResponse>(SystemActions.ExecFuncAnonymous, args).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a custom method; local calls only.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public async Task<ExecFuncResponse> ExecFuncLocalAsync(ExecFuncRequest args)
        {
            return await ExecuteAsync<ExecFuncResponse>(SystemActions.ExecFuncLocal, args).ConfigureAwait(false);
        }
    }
}
