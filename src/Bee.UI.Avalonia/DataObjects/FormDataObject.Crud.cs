using Bee.Api.Client.Connectors;
using Bee.Definition;

namespace Bee.UI.Avalonia.DataObjects
{
    /// <summary>
    /// Asynchronous CRUD half of <see cref="FormDataObject"/> (load / save / delete / new against the
    /// backend connector). Split out for file size only; behaviour is unchanged.
    /// </summary>
    public partial class FormDataObject
    {
        /// <summary>
        /// Loads the master row (and its details) identified by <paramref name="rowId"/>
        /// from the backend BO and replaces the local <see cref="DataSet"/>.
        /// </summary>
        /// <param name="rowId">The master row identifier (<c>sys_rowid</c>).</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no <see cref="FormApiConnector"/> was supplied to the constructor,
        /// or when the server responds with a null <see cref="DataSet"/> (no row matched).
        /// </exception>
        public async Task LoadAsync(Guid rowId)
        {
            var connector = RequireConnector(nameof(LoadAsync));

            IsLoading = true;
            try
            {
                // NOTE: No ConfigureAwait(false) here or in the other CRUD methods —
                // the continuation mutates the DataSet and raises change events that
                // Avalonia controls consume, and those are thread-affine, so it must
                // resume on the captured UI context.
                var response = await connector.GetDataAsync(rowId);
                if (response.DataSet is null)
                    throw new InvalidOperationException(
                        $"No master row found for {SysFields.RowId} = {rowId}.");

                ReplaceDataSet(response.DataSet);
                IsDirty = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Persists the current <see cref="DataSet"/> through the backend BO and replaces
        /// the local <see cref="DataSet"/> with the refreshed copy returned by the server
        /// (so that server-generated columns surface back to the caller).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no <see cref="FormApiConnector"/> was supplied to the constructor.
        /// </exception>
        public async Task SaveAsync()
        {
            var connector = RequireConnector(nameof(SaveAsync));

            IsLoading = true;
            try
            {
                var response = await connector.SaveAsync(DataSet);
                if (response.DataSet is not null)
                    ReplaceDataSet(response.DataSet);
                IsDirty = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes the current master row through the backend BO and resets the local
        /// <see cref="DataSet"/> to the empty schema-derived skeleton.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no <see cref="FormApiConnector"/> was supplied to the constructor,
        /// when there is no master row to delete, or when the master table does not carry
        /// a <c>sys_rowid</c> column.
        /// </exception>
        public async Task DeleteAsync()
        {
            var connector = RequireConnector(nameof(DeleteAsync));
            var rowId = RequireMasterRowId();

            IsLoading = true;
            try
            {
                await connector.DeleteAsync(rowId);
                ReplaceDataSet(BuildEmptyDataSet(_schema));
                IsDirty = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Requests a blank <see cref="DataSet"/> skeleton seeded with FormSchema defaults
        /// and a server-issued <c>sys_rowid</c> from the backend BO, and replaces the
        /// local <see cref="DataSet"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no <see cref="FormApiConnector"/> was supplied to the constructor,
        /// or when the server responds with a null <see cref="DataSet"/>.
        /// </exception>
        public async Task NewAsync()
        {
            var connector = RequireConnector(nameof(NewAsync));

            IsLoading = true;
            try
            {
                var response = await connector.GetNewDataAsync();
                if (response.DataSet is null)
                    throw new InvalidOperationException(
                        "GetNewData returned a null DataSet; cannot initialize a new master row.");

                ReplaceDataSet(response.DataSet);
                IsDirty = false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
