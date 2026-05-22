using System.Data;
using System.Globalization;
using Bee.Api.Client.Connectors;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Web.Blazor.Server.DataObjects
{
    /// <summary>
    /// Holds the in-memory <see cref="System.Data.DataSet"/> for a single form view
    /// (one master row plus zero or more detail tables) and exposes a small string-based
    /// access surface for two-way binding by <c>DynamicForm</c> / <c>DynamicGrid</c>.
    /// </summary>
    /// <remarks>
    /// Phase 1a only owns the local <see cref="DataSet"/> shape derived from
    /// <see cref="FormSchema"/> together with <see cref="GetField"/> / <see cref="SetField"/>
    /// round-trip. Server round-trip (<c>LoadAsync</c> / <c>SaveAsync</c> / <c>DeleteAsync</c> /
    /// <c>NewAsync</c>) is deferred to Phase 1b once the BO CRUD methods land.
    /// </remarks>
    public class FormDataObject
    {
        private const string Phase1bMessage =
            "Phase 1b: implemented once the BO CRUD methods plan lands.";

        private readonly FormSchema _schema;
#pragma warning disable IDE0052 // Phase 1b will dispatch BO calls through this connector.
        private readonly FormApiConnector? _connector;
#pragma warning restore IDE0052

        /// <summary>
        /// Initializes a new instance of <see cref="FormDataObject"/> and derives the
        /// empty <see cref="DataSet"/> shape from <paramref name="schema"/>.
        /// </summary>
        /// <param name="schema">The form schema that drives column derivation.</param>
        /// <param name="connector">The connector used for Phase 1b server round-trips. Optional during Phase 1a.</param>
        public FormDataObject(FormSchema schema, FormApiConnector? connector = null)
        {
            ArgumentNullException.ThrowIfNull(schema);
            if (string.IsNullOrWhiteSpace(schema.ProgId))
                throw new ArgumentException("FormSchema.ProgId must not be empty.", nameof(schema));

            _schema = schema;
            _connector = connector;
            DataSet = BuildEmptyDataSet(schema);
        }

        /// <summary>
        /// Gets the underlying dataset that holds the master row and detail tables.
        /// </summary>
        public DataSet DataSet { get; }

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
        /// Gets a value indicating whether an asynchronous load is currently in progress.
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
            return FormatForBinding(raw);
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
            row[fieldName] = ConvertToColumnValue(value, column);
            IsDirty = true;
        }

        /// <summary>
        /// Creates an empty master row populated with column defaults, replacing any
        /// existing row. Used by Phase 1a tests and by <see cref="NewAsync"/> in Phase 1b.
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
        /// Loads the form data for the given query arguments from the backend BO.
        /// </summary>
        public static Task LoadAsync(object queryArgs)
            => throw new NotImplementedException(Phase1bMessage);

        /// <summary>
        /// Persists the current dataset to the backend BO.
        /// </summary>
        public static Task SaveAsync()
            => throw new NotImplementedException(Phase1bMessage);

        /// <summary>
        /// Deletes the current master record via the backend BO.
        /// </summary>
        public Task DeleteAsync()
            => throw new NotImplementedException(Phase1bMessage);

        /// <summary>
        /// Initializes a new master record, calling the backend BO for any server-side defaults.
        /// </summary>
        public Task NewAsync()
            => throw new NotImplementedException(Phase1bMessage);

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
                // ISO 8601 keeps round-trip parity with HTML date/datetime-local inputs.
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty,
            };
        }

        private static object ConvertToColumnValue(string? value, DataColumn column)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Schemas built by DataTableExtensions.AddColumn pin AllowDBNull=false for
                // any FieldDbType that exposes a non-null default; respect that contract
                // by falling back to the column default rather than forcing DBNull.
                return column.AllowDBNull ? DBNull.Value : column.DefaultValue;
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
    }
}
