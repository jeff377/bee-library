using System.Data;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.Web.Blazor.Server.DataObjects
{
    /// <summary>
    /// Holds the in-memory <see cref="System.Data.DataSet"/> for a single form view
    /// (one master row plus zero or more detail tables) and exposes a small string-based
    /// access surface for two-way binding by <c>DynamicForm</c> / <c>DynamicGrid</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The constructor seeds an empty <see cref="DataSet"/> derived from
    /// <see cref="FormSchema"/>. The async methods (<see cref="LoadAsync"/>,
    /// <see cref="NewAsync"/>, <see cref="SaveAsync"/>, <see cref="DeleteAsync"/>)
    /// round-trip through the supplied <see cref="FormApiConnector"/> and replace
    /// the local <see cref="DataSet"/> with the server response.
    /// </para>
    /// <para>
    /// The value rules this type used to carry privately — DataSet seeding, string/column
    /// coercion, display formatting, the CRUD preconditions — now live once in
    /// <c>Bee.Api.Client</c> (<c>FormValueBinding</c> and <c>FormDataGuard</c>), which both this
    /// head and <c>Bee.Web.Blazor.Server</c> already reference. They are head-agnostic: the same
    /// <see cref="FormSchema"/> and the same server responses drive every head.
    /// </para>
    /// <para>
    /// NOTE: this used to be a verbatim copy of the Avalonia head's version, marked "deliberately
    /// parallel" on the grounds that the only shared home would be <c>Bee.UI.Core</c> — which
    /// would put <c>Bee.Web.Blazor.Server</c> inside the <c>Bee.UI.*</c> family, and Blazor is a
    /// separate family on purpose. That reasoning had a hole: <c>Bee.Api.Client</c> is a common
    /// ancestor of both heads and carries no such family meaning. It also had a cost — the note
    /// said nothing enforced the parallel, and the two copies did in fact diverge, with one side
    /// writing <see cref="DBNull"/> into a non-nullable column long after the other had fixed it.
    /// </para>
    /// </remarks>
    public class FormDataObject
    {
        private readonly FormSchema _schema;
        private readonly FormApiConnector? _connector;

        /// <summary>
        /// Initializes a new instance of <see cref="FormDataObject"/> and derives the
        /// empty <see cref="DataSet"/> shape from <paramref name="schema"/>.
        /// </summary>
        /// <param name="schema">The form schema that drives column derivation.</param>
        /// <param name="connector">
        /// The connector used for server round-trips (<see cref="LoadAsync"/>,
        /// <see cref="NewAsync"/>, <see cref="SaveAsync"/>, <see cref="DeleteAsync"/>).
        /// Optional when only the in-memory <see cref="DataSet"/> surface is needed.
        /// </param>
        public FormDataObject(FormSchema schema, FormApiConnector? connector = null)
        {
            ArgumentNullException.ThrowIfNull(schema);
            if (string.IsNullOrWhiteSpace(schema.ProgId))
                throw new ArgumentException("FormSchema.ProgId must not be empty.", nameof(schema));

            _schema = schema;
            _connector = connector;
            DataSet = FormValueBinding.BuildEmptyDataSet(schema);
        }

        /// <summary>
        /// Gets the underlying dataset that holds the master row and detail tables.
        /// </summary>
        public DataSet DataSet { get; private set; }

        /// <summary>
        /// Gets the master <see cref="DataTable"/> (the table whose name equals
        /// <see cref="FormSchema.ProgId"/>).
        /// </summary>
        public DataTable MasterTable => DataSet.GetMasterTable()
            ?? throw new InvalidOperationException("Master table is missing from the dataset.");

        /// <summary>
        /// Gets the first row of the master table, or <c>null</c> if no master row exists yet.
        /// </summary>
        public DataRow? MasterRow => MasterTable.IsEmpty() ? null : MasterTable.Rows[0];

        /// <summary>
        /// Gets the detail tables (all dataset tables other than the master table).
        /// </summary>
        public IEnumerable<DataTable> DetailTables
        {
            get
            {
                var master = MasterTable;
                foreach (DataTable table in DataSet.Tables)
                {
                    if (!ReferenceEquals(table, master))
                        yield return table;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether an asynchronous round-trip is in progress.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the master row has been modified since the
        /// last load/save.
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Reads the field value from the master row and renders it as a string suitable
        /// for HTML <c>value</c> attributes. Returns an empty string when the master row
        /// is absent, the field does not exist, or the value is <see cref="DBNull"/>.
        /// </summary>
        /// <param name="fieldName">The field (column) name.</param>
        public string GetField(string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            var row = MasterRow;
            if (row is null) return string.Empty;
            if (!row.Table.Columns.Contains(fieldName)) return string.Empty;

            var raw = row[fieldName];
            return FormValueBinding.ToBindingString(raw);
        }

        /// <summary>
        /// Writes <paramref name="value"/> to the master row after coercing it to the
        /// column's declared CLR type. An empty input becomes <see cref="DBNull"/>.
        /// </summary>
        /// <param name="fieldName">The field (column) name.</param>
        /// <param name="value">The string value supplied by the bound input.</param>
        public void SetField(string fieldName, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            var row = MasterRow;
            if (row is null) return;
            if (!row.Table.Columns.Contains(fieldName)) return;

            var column = row.Table.Columns[fieldName]!;
            row[fieldName] = FormValueBinding.ToColumnValue(value, column);
            IsDirty = true;
        }

        /// <summary>
        /// Creates an empty master row populated with column defaults, replacing any
        /// existing row. Used when callers want a blank skeleton without going through
        /// the backend (e.g. unit tests or offline previews); production flows should
        /// prefer <see cref="NewAsync"/> so the server can seed defaults and
        /// <c>sys_rowid</c>.
        /// </summary>
        public void InitializeNewMaster()
        {
            MasterTable.Rows.Clear();
            MasterTable.Rows.Add(MasterTable.NewRow());
            IsDirty = false;
        }

        /// <summary>
        /// Looks up the <see cref="FormField"/> metadata on the master table for the given
        /// field name, or <c>null</c> if no such field exists. UI components use this to
        /// reach metadata that the layout-level types do not carry (for example,
        /// <see cref="FormField.ListItems"/> for dropdowns).
        /// </summary>
        /// <param name="fieldName">The field (column) name.</param>
        public FormField? GetFormField(string fieldName)
        {
            var master = _schema.MasterTable;
            if (master?.Fields is null) return null;
            return master.Fields.Contains(fieldName) ? master.Fields[fieldName] : null;
        }

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
            var connector = FormDataGuard.RequireConnector(_connector, nameof(LoadAsync));

            IsLoading = true;
            try
            {
                var response = await connector.GetDataAsync(rowId).ConfigureAwait(false);
                if (response.DataSet is null)
                    throw new InvalidOperationException(
                        $"No master row found for {SysFields.RowId} = {rowId}.");

                DataSet = response.DataSet;
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
            var connector = FormDataGuard.RequireConnector(_connector, nameof(SaveAsync));

            IsLoading = true;
            try
            {
                var response = await connector.SaveAsync(DataSet).ConfigureAwait(false);
                if (response.DataSet is not null)
                    DataSet = response.DataSet;
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
            var connector = FormDataGuard.RequireConnector(_connector, nameof(DeleteAsync));
            var rowId = FormDataGuard.RequireMasterRowId(MasterRow);

            IsLoading = true;
            try
            {
                await connector.DeleteAsync(rowId).ConfigureAwait(false);
                DataSet = FormValueBinding.BuildEmptyDataSet(_schema);
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
            var connector = FormDataGuard.RequireConnector(_connector, nameof(NewAsync));

            IsLoading = true;
            try
            {
                var response = await connector.GetNewDataAsync().ConfigureAwait(false);
                if (response.DataSet is null)
                    throw new InvalidOperationException(
                        "GetNewData returned a null DataSet; cannot initialize a new master row.");

                DataSet = response.DataSet;
                IsDirty = false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
