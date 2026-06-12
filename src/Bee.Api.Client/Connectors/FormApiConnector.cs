using System.Data;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.Form;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Paging;
using Bee.Definition.Sorting;

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

        /// <summary>
        /// Asynchronously retrieves list-view rows from the master table of <see cref="ProgId"/>.
        /// </summary>
        /// <param name="selectFields">
        /// The comma-separated field names to retrieve; an empty value falls back to
        /// <c>FormSchema.ListFields</c>, then to all fields.
        /// </param>
        /// <param name="filter">The filter condition tree; <c>null</c> for an unfiltered query.</param>
        /// <param name="sortFields">The sort field collection; <c>null</c> uses the default ordering.</param>
        /// <param name="paging">The paging options; <c>null</c> returns every matching row.</param>
        /// <remarks>
        /// When <paramref name="paging"/> is <c>null</c> callers should supply a
        /// <paramref name="filter"/> that bounds the result set, otherwise an
        /// unbounded query against a large table loads every matching row into memory
        /// on both the server and the client. Pass a <see cref="PagingOptions"/> to
        /// page through large result sets.
        /// </remarks>
        public virtual async Task<GetListResponse> GetListAsync(
            string selectFields = "",
            FilterNode? filter = null,
            SortFieldCollection? sortFields = null,
            PagingOptions? paging = null)
        {
            var request = new GetListRequest
            {
                SelectFields = selectFields,
                Filter = filter,
                SortFields = sortFields,
                Paging = paging,
            };
            return await ExecuteAsync<GetListResponse>(FormActions.GetList, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously retrieves lookup candidate rows for picker windows that
        /// reference <see cref="ProgId"/>. The projection is server-resolved from
        /// <c>FormSchema.LookupFields</c> (falling back to <c>sys_id</c> / <c>sys_name</c>)
        /// and always includes <c>sys_rowid</c>; the caller cannot widen it.
        /// </summary>
        /// <param name="searchText">
        /// The search text matched server-side against the string-typed lookup fields;
        /// an empty value applies no search filter.
        /// </param>
        /// <param name="paging">The paging options; <c>null</c> applies the server-side default page size.</param>
        public virtual async Task<GetLookupResponse> GetLookupAsync(
            string searchText = "",
            PagingOptions? paging = null)
        {
            var request = new GetLookupRequest
            {
                SearchText = searchText,
                Paging = paging,
            };
            return await ExecuteAsync<GetLookupResponse>(FormActions.GetLookup, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously requests a blank <c>DataSet</c> skeleton seeded with
        /// FormSchema defaults and a server-issued <c>sys_rowid</c>; step 1 of
        /// the new-and-save flow.
        /// </summary>
        public virtual async Task<GetNewDataResponse> GetNewDataAsync()
        {
            var request = new GetNewDataRequest();
            return await ExecuteAsync<GetNewDataResponse>(FormActions.GetNewData, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously loads the master row (and its details) by
        /// <paramref name="rowId"/>; step 1 of the load-and-save flow.
        /// </summary>
        /// <param name="rowId">The master row identifier (<c>sys_rowid</c>).</param>
        public virtual async Task<GetDataResponse> GetDataAsync(Guid rowId)
        {
            var request = new GetDataRequest { RowId = rowId };
            return await ExecuteAsync<GetDataResponse>(FormActions.GetData, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously persists a <c>DataSet</c> by dispatching
        /// INSERT / UPDATE / DELETE based on each row's <c>RowState</c>; step 2
        /// of both the new-and-save and load-and-save flows.
        /// </summary>
        /// <param name="dataSet">The DataSet to persist.</param>
        public virtual async Task<SaveResponse> SaveAsync(DataSet dataSet)
        {
            ArgumentNullException.ThrowIfNull(dataSet);
            var request = new SaveRequest { DataSet = dataSet };
            return await ExecuteAsync<SaveResponse>(FormActions.Save, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously deletes a single master row directly by
        /// <paramref name="rowId"/> without first loading the full
        /// <c>DataSet</c>.
        /// </summary>
        /// <param name="rowId">The master row identifier (<c>sys_rowid</c>).</param>
        public virtual async Task<DeleteResponse> DeleteAsync(Guid rowId)
        {
            var request = new DeleteRequest { RowId = rowId };
            return await ExecuteAsync<DeleteResponse>(FormActions.Delete, request).ConfigureAwait(false);
        }
    }
}
