using System.Data;
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
    public partial class FormDataObject
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

        private bool _isDirty;

        /// <summary>
        /// Gets a value indicating whether the data has been modified since the last
        /// load/save. Changing this raises <see cref="IsDirtyChanged"/> only on an actual
        /// transition (assigning the same value raises nothing).
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value) return;
                _isDirty = value;
                IsDirtyChanged?.Invoke(this, EventArgs.Empty);
            }
        }

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
        /// Raised after a row is added to any table of the <see cref="DataSet"/> (e.g. a
        /// detail grid's add action). Bridged from <see cref="DataTable.RowChanged"/>.
        /// </summary>
        public event EventHandler<RowChangedEventArgs>? RowAdded;

        /// <summary>
        /// Raised after a row is deleted from any table of the <see cref="DataSet"/> (e.g. a
        /// detail grid's delete action). Bridged from <see cref="DataTable.RowDeleted"/>.
        /// </summary>
        public event EventHandler<RowChangedEventArgs>? RowDeleted;

        /// <summary>
        /// Raised when <see cref="IsDirty"/> transitions (clean ↔ dirty). Drives Save-button
        /// enablement / unsaved-changes prompts without polling.
        /// </summary>
        public event EventHandler? IsDirtyChanged;

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
    }
}
