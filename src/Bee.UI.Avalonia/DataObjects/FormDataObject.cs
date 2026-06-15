using System.Data;
using System.Globalization;
using Bee.Api.Client.Connectors;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.UI.Avalonia.DataObjects
{
    /// <summary>
    /// Holds the in-memory <see cref="System.Data.DataSet"/> for a single form view
    /// (one master row plus zero or more detail tables) and exposes a small string-based
    /// access surface for two-way binding by <c>DynamicForm</c> / <c>DynamicGrid</c>.
    /// </summary>
    /// <remarks>
    /// The constructor seeds an empty <see cref="DataSet"/> derived from
    /// <see cref="FormSchema"/>. The async methods (<see cref="LoadAsync"/>,
    /// <see cref="NewAsync"/>, <see cref="SaveAsync"/>, <see cref="DeleteAsync"/>)
    /// round-trip through the supplied <see cref="FormApiConnector"/> and replace
    /// the local <see cref="DataSet"/> with the server response.
    /// </remarks>
    public class FormDataObject
    {
        private readonly FormSchema _schema;
        private readonly FormApiConnector? _connector;
        // Rows under an explicit BeginRowEdit session. ADO.NET does NOT suppress
        // ColumnChanged during BeginEdit (pinned by test) — the bridge consults this
        // set to stay silent until CommitRowEdit re-publishes the session's changes.
        private readonly HashSet<DataRow> _rowsInEdit = [];

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
            DataSet = null!;
            ReplaceDataSet(BuildEmptyDataSet(schema), notify: false);
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
        /// Raised after any field value changes in any table of the
        /// <see cref="DataSet"/> — master and detail alike, regardless of the write
        /// path (<see cref="SetField(string, string?)"/>, grid cell editors, lookup write-backs,
        /// direct <see cref="DataRow"/> writes). Bridged from the ADO.NET
        /// <see cref="DataTable.ColumnChanged"/> event so no writer has to remember
        /// to raise it.
        /// </summary>
        public event EventHandler<FieldValueChangedEventArgs>? FieldValueChanged;

        /// <summary>
        /// Raised after the underlying <see cref="DataSet"/> is replaced or reset
        /// (<see cref="LoadAsync"/>, <see cref="SaveAsync"/>, <see cref="DeleteAsync"/>,
        /// <see cref="NewAsync"/>, <see cref="InitializeNewMaster"/>). Bound editors
        /// re-pull their values instead of requiring a full visual rebuild.
        /// </summary>
        public event EventHandler? DataSetReplaced;

        /// <summary>
        /// Reads the field value from the master row and renders it as a string suitable
        /// for two-way bound input controls. Returns an empty string when the master row
        /// is absent, the field does not exist, or the value is <see cref="DBNull"/>.
        /// </summary>
        /// <param name="fieldName">The field (column) name.</param>
        public string GetField(string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            var row = MasterRow;
            if (row is null) return string.Empty;
            return GetField(row, fieldName);
        }

        /// <summary>
        /// Reads the field value from <paramref name="row"/> (master or detail) and
        /// renders it as a binding string. While the row is in an edit session
        /// (<see cref="BeginRowEdit"/>), the proposed value is returned.
        /// </summary>
        /// <param name="row">The row to read from.</param>
        /// <param name="fieldName">The field (column) name.</param>
        public string GetField(DataRow row, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(row);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            EnsureOwnedRow(row);

            if (!row.Table.Columns.Contains(fieldName)) return string.Empty;
            return FormatForBinding(row[fieldName]);
        }

        /// <summary>
        /// Writes <paramref name="value"/> to the master row after coercing it to the
        /// column's declared CLR type. An empty input falls back to the column default
        /// (or <see cref="DBNull"/> when the column allows nulls).
        /// </summary>
        /// <remarks>
        /// Idempotent against initial-render echoes — Avalonia (and to a lesser extent
        /// MAUI) controls fire <c>TextChanged</c> once with the value we just set via
        /// the control's object initializer, before the user has touched anything.
        /// Re-writing that echo would dirty the row and, on NOT-NULL columns whose
        /// <see cref="DataColumn.DefaultValue"/> is still <see cref="DBNull"/> (typical
        /// for tables that round-trip through the wire and never went through
        /// <c>DataTableExtensions.AddColumn</c>), raise <see cref="NoNullAllowedException"/>
        /// during <c>EndEdit</c>. Comparing against the existing value first short-
        /// circuits both hazards.
        /// </remarks>
        /// <param name="fieldName">The field (column) name.</param>
        /// <param name="value">The string value supplied by the bound input.</param>
        public void SetField(string fieldName, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            var row = MasterRow;
            if (row is null) return;
            SetField(row, fieldName, value);
        }

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="row"/> (master or
        /// detail) after coercing it to the column's declared CLR type, with the
        /// same empty-input and echo-guard semantics as the master overload.
        /// </summary>
        /// <param name="row">The row to write to.</param>
        /// <param name="fieldName">The field (column) name.</param>
        /// <param name="value">The string value supplied by the bound input.</param>
        public void SetField(DataRow row, string fieldName, string? value)
        {
            ArgumentNullException.ThrowIfNull(row);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            EnsureOwnedRow(row);

            if (!row.Table.Columns.Contains(fieldName)) return;

            var column = row.Table.Columns[fieldName]!;
            var newValue = ConvertToColumnValue(value, column);
            if (Equals(newValue, row[fieldName])) return;

            // The write itself raises FieldValueChanged and marks dirty through the
            // DataTable event bridge; the compare-first guard above keeps no-op
            // writes (initial-render echoes) from producing event noise.
            row[fieldName] = newValue;
        }

        /// <summary>
        /// Starts a buffered edit session on <paramref name="row"/>: subsequent
        /// writes go to the row's proposed version and ADO.NET suspends change
        /// events until the session ends.
        /// </summary>
        /// <param name="row">The row to edit.</param>
        public void BeginRowEdit(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            EnsureOwnedRow(row);
            _rowsInEdit.Add(row);
            row.BeginEdit();
        }

        /// <summary>
        /// Commits a buffered edit session: captures which fields the session
        /// actually changed (proposed vs current — taken before <c>EndEdit</c>
        /// merges them), ends the edit, then re-publishes the per-field
        /// <see cref="FieldValueChanged"/> events ADO.NET suppressed during the
        /// session. Dirty tracking flows through the event bridge.
        /// </summary>
        /// <param name="row">The row whose edit session to commit.</param>
        public void CommitRowEdit(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            EnsureOwnedRow(row);
            _rowsInEdit.Remove(row);

            if (!row.HasVersion(DataRowVersion.Proposed))
            {
                row.EndEdit();
                return;
            }

            var changedFields = new List<string>();
            foreach (DataColumn column in row.Table.Columns)
            {
                if (!Equals(row[column, DataRowVersion.Proposed], row[column, DataRowVersion.Current]))
                    changedFields.Add(column.ColumnName);
            }

            if (changedFields.Count == 0)
            {
                // BeginEdit creates the proposed version eagerly, so EndEdit would
                // raise RowChanged (and dirty the object) even though nothing
                // changed; cancelling is equivalent and stays silent.
                row.CancelEdit();
                return;
            }

            row.EndEdit();

            foreach (var fieldName in changedFields)
            {
                FieldValueChanged?.Invoke(this, new FieldValueChangedEventArgs(
                    row.Table.TableName, fieldName, GetField(row, fieldName), row));
            }
        }

        /// <summary>
        /// Cancels a buffered edit session, restoring every field to its pre-session
        /// value. No events are raised — nothing changed.
        /// </summary>
        /// <param name="row">The row whose edit session to cancel.</param>
        public void CancelRowEdit(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            EnsureOwnedRow(row);
            _rowsInEdit.Remove(row);
            row.CancelEdit();
        }

        private void EnsureOwnedRow(DataRow row)
        {
            if (!ReferenceEquals(row.Table.DataSet, DataSet))
                throw new ArgumentException("Row does not belong to this data object's DataSet.", nameof(row));
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
            // The dataset instance is unchanged (no resubscription needed), but the
            // visible content was reset — notify so bound editors re-pull.
            DataSetReplaced?.Invoke(this, EventArgs.Empty);
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
        /// Looks up the <see cref="FormField"/> metadata on the named table (master or
        /// detail), or <c>null</c> if the table or field does not exist. Detail-grid
        /// editors use this to reach per-column metadata such as
        /// <see cref="FormField.ListItems"/>.
        /// </summary>
        /// <param name="tableName">The schema table name.</param>
        /// <param name="fieldName">The field (column) name.</param>
        public FormField? GetFormField(string tableName, string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            var table = _schema.Tables?.GetOrDefault(tableName);
            if (table?.Fields is null) return null;
            return table.Fields.Contains(fieldName) ? table.Fields[fieldName] : null;
        }


        /// <summary>
        /// Applies a lookup selection to the bound row: writes the selected row's
        /// <c>sys_rowid</c> into <paramref name="field"/> and copies each mapped source
        /// field to its local destination field. <see cref="FormField.LookupFieldMappings"/>
        /// wins over <see cref="FormField.RelationFieldMappings"/> when both are declared.
        /// </summary>
        /// <remarks>
        /// The locally written <c>ref_*</c> values are display-only between saves — a
        /// reload re-derives them from the server-side relation JOIN, which stays the
        /// single source of truth.
        /// </remarks>
        /// <param name="field">The relation field the lookup belongs to.</param>
        /// <param name="selectedRow">The row picked in the lookup window.</param>
        /// <param name="targetRow">The detail row to write to; <c>null</c> targets the master row.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="selectedRow"/> lacks <c>sys_rowid</c> or a mapped
        /// source field — the mapping must stay within the target form's lookup field set.
        /// </exception>
        public void ApplyLookupSelection(FormField field, DataRow selectedRow, DataRow? targetRow = null)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(selectedRow);

            if (!selectedRow.Table.Columns.Contains(SysFields.RowId))
                throw new InvalidOperationException("The lookup selection is missing the sys_rowid column.");

            SetLookupField(targetRow, field.FieldName, FormatForBinding(selectedRow[SysFields.RowId]));

            foreach (var mapping in ResolveLookupMappings(field))
            {
                // A silently skipped mapping would leave stale display values behind,
                // so a source field outside the lookup result is a hard error.
                if (!selectedRow.Table.Columns.Contains(mapping.SourceField))
                    throw new InvalidOperationException(
                        $"Lookup source field '{mapping.SourceField}' is not in the lookup result; " +
                        "declare it in the target form's LookupFields.");
                SetLookupField(targetRow, mapping.DestinationField, FormatForBinding(selectedRow[mapping.SourceField]));
            }
        }

        /// <summary>
        /// Clears a lookup selection: resets <paramref name="field"/> and every mapped
        /// destination field to the column default (empty input semantics of
        /// <see cref="SetField(string, string?)"/>).
        /// </summary>
        /// <param name="field">The relation field the lookup belongs to.</param>
        /// <param name="targetRow">The detail row to write to; <c>null</c> targets the master row.</param>
        public void ClearLookupSelection(FormField field, DataRow? targetRow = null)
        {
            ArgumentNullException.ThrowIfNull(field);

            SetLookupField(targetRow, field.FieldName, null);
            foreach (var mapping in ResolveLookupMappings(field))
                SetLookupField(targetRow, mapping.DestinationField, null);
        }

        private void SetLookupField(DataRow? targetRow, string fieldName, string? value)
        {
            if (targetRow is not null)
                SetField(targetRow, fieldName, value);
            else
                SetField(fieldName, value);
        }

        private static FieldMappingCollection ResolveLookupMappings(FormField field)
        {
            if (field.LookupFieldMappings is { Count: > 0 } lookupMappings)
                return lookupMappings;
            if (field.RelationFieldMappings is { Count: > 0 } relationMappings)
                return relationMappings;
            return [];
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

        /// <summary>
        /// The single assignment point for <see cref="DataSet"/>: moves the table
        /// event subscriptions from the old dataset to the new one (subscribing only
        /// after the server has fully populated it, so loading raises nothing) and
        /// notifies subscribers.
        /// </summary>
        private void ReplaceDataSet(DataSet dataSet, bool notify = true)
        {
            DetachTableEvents(DataSet);
            _rowsInEdit.Clear();
            DataSet = dataSet;
            AttachTableEvents(dataSet);
            if (notify)
                DataSetReplaced?.Invoke(this, EventArgs.Empty);
        }

        private void AttachTableEvents(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                table.TableNewRow += OnTableNewRow;
                table.ColumnChanged += OnTableColumnChanged;
                table.RowChanged += OnTableRowChanged;
                table.RowDeleted += OnTableRowDeleted;
            }
        }

        private void DetachTableEvents(DataSet? dataSet)
        {
            if (dataSet is null) return;
            foreach (DataTable table in dataSet.Tables)
            {
                table.TableNewRow -= OnTableNewRow;
                table.ColumnChanged -= OnTableColumnChanged;
                table.RowChanged -= OnTableRowChanged;
                table.RowDeleted -= OnTableRowDeleted;
            }
        }

        /// <summary>
        /// Seeds a freshly created row's persisted columns with type-appropriate non-null values
        /// drawn from the FormSchema, so a new row never reaches the database with a NULL that would
        /// violate a NOT NULL constraint. <c>sys_rowid</c> gets a fresh key, <c>sys_master_rowid</c>
        /// links to the loaded master, and other columns default by <see cref="FieldDbType"/>
        /// (text → empty string, numeric → 0, Date → today, DateTime → now, …). Columns that already
        /// carry a value (e.g. a schema-pinned default) are left untouched.
        /// </summary>
        private void OnTableNewRow(object? sender, DataTableNewRowEventArgs e)
        {
            var formTable = _schema.Tables?.GetOrDefault(e.Row.Table.TableName);
            if (formTable is null) return;

            // Same schema-driven seeding the server uses for the GetNewData master row, plus the
            // master link for a detail row (the client knows the loaded master).
            FormRowDefaults.Apply(formTable, e.Row, ResolveMasterRowId());
        }

        // The master row's sys_rowid a new detail row links through sys_master_rowid. Returns the
        // raw value (Guid or the provider's exact string form) so the link preserves the master's
        // casing — a re-parsed Guid would lowercase a string key and orphan the detail under a
        // case-sensitive comparison (e.g. SQLite stores GUIDs as case-sensitive TEXT).
        private object? ResolveMasterRowId()
        {
            var master = MasterRow;
            if (master is null || !master.Table.Columns.Contains(SysFields.RowId)) return null;
            var value = master[SysFields.RowId];
            return value == DBNull.Value ? null : value;
        }

        private void OnTableColumnChanged(object? sender, DataColumnChangeEventArgs e)
        {
            // Seeding a detached row (NewRow before Rows.Add) stays silent; attaching
            // the row marks dirty through RowChanged instead.
            if (e.Row.RowState == DataRowState.Detached) return;
            // Rows under an explicit edit session publish nothing until commit —
            // CommitRowEdit re-publishes the session's changes; a cancelled session
            // must leak no events for values that were rolled back.
            if (_rowsInEdit.Contains(e.Row)) return;

            IsDirty = true;
            FieldValueChanged?.Invoke(this, new FieldValueChangedEventArgs(
                ((DataTable)sender!).TableName,
                e.Column!.ColumnName,
                FormatForBinding(e.ProposedValue),
                e.Row));
        }

        private void OnTableRowChanged(object? sender, DataRowChangeEventArgs e)
        {
            // Only data mutations dirty the object; framework actions (AcceptChanges
            // raises Commit, RejectChanges raises Rollback) do not. Rows in an
            // explicit edit session dirty the object at commit, not per keystroke.
            if (_rowsInEdit.Contains(e.Row)) return;
            if (e.Action is DataRowAction.Add or DataRowAction.Change)
                IsDirty = true;
        }

        private void OnTableRowDeleted(object? sender, DataRowChangeEventArgs e)
        {
            IsDirty = true;
        }

        private FormApiConnector RequireConnector(string operation)
        {
            return _connector
                ?? throw new InvalidOperationException(
                    $"{operation} requires a FormApiConnector; pass one to the FormDataObject constructor.");
        }

        private Guid RequireMasterRowId()
        {
            var row = MasterRow
                ?? throw new InvalidOperationException("No master row is loaded; cannot delete.");
            if (!row.Table.Columns.Contains(SysFields.RowId))
                throw new InvalidOperationException(
                    $"Master table is missing the '{SysFields.RowId}' column; cannot delete.");

            var raw = row[SysFields.RowId];
            if (raw is null || raw == DBNull.Value)
                throw new InvalidOperationException(
                    $"Master row has a null '{SysFields.RowId}'; cannot delete.");

            return raw is Guid g ? g : Guid.Parse(raw.ToString()!);
        }

        private static DataSet BuildEmptyDataSet(FormSchema schema)
        {
            var dataSet = new DataSet(schema.ProgId);

            if (schema.Tables is null)
                return dataSet;

            var masterTable = schema.MasterTable;
            foreach (var table in schema.Tables)
            {
                var dataTable = new DataTable(table.TableName);
                if (table.Fields is not null)
                {
                    foreach (var field in table.Fields)
                        dataTable.AddColumn(field.FieldName, field.DbType);
                }
                dataSet.Tables.Add(dataTable);
            }

            if (masterTable is not null && !dataSet.Tables.Contains(masterTable.TableName))
            {
                var dataTable = new DataTable(masterTable.TableName);
                dataSet.Tables.Add(dataTable);
            }

            return dataSet;
        }

        private static string FormatForBinding(object? raw)
        {
            if (raw is null || raw == DBNull.Value)
                return string.Empty;

            return raw switch
            {
                // ISO 8601 keeps round-trip parity with desktop DatePicker / TextBox controls.
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty,
            };
        }

        // NOTE: Internal (not private) so GridControl's in-cell editors reuse the same
        // string-to-column coercion rules instead of growing a divergent copy.
        internal static object ConvertToColumnValue(string? value, DataColumn column)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (column.AllowDBNull) return DBNull.Value;

                // Non-nullable column: prefer the column's own DefaultValue when it
                // was properly seeded (DataTableExtensions.AddColumn pins this to a
                // type-appropriate non-null for every FieldDbType). Server-side
                // responses often arrive with raw ADO.NET columns whose DefaultValue
                // is still DBNull — for those, synthesise a non-null fallback from
                // the column's CLR type rather than writing DBNull into a NOT NULL
                // column, which would raise NoNullAllowedException on EndEdit.
                if (column.DefaultValue is not null && column.DefaultValue != DBNull.Value)
                    return column.DefaultValue;
                return ResolveEmptyValueForType(column.DataType);
            }

            var targetType = column.DataType;
            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(Guid))
                return Guid.Parse(value);
            if (targetType == typeof(byte[]))
                return Convert.FromBase64String(value);
            if (targetType == typeof(DateTime))
                return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        internal static object ResolveEmptyValueForType(Type targetType)
        {
            if (targetType == typeof(string)) return string.Empty;
            if (targetType == typeof(Guid)) return Guid.Empty;
            if (targetType == typeof(DateTime)) return DateTime.MinValue;
            if (targetType == typeof(byte[])) return Array.Empty<byte>();
            if (targetType.IsValueType) return Activator.CreateInstance(targetType)!;
            return DBNull.Value;
        }
    }
}
