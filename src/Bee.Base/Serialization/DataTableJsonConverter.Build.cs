using System.Data;
using Bee.Base.Data;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Materialising a `DataTable` from the shapes the read side produced.
    /// </summary>
    /// <remarks>
    /// Separate from reading because it is pure construction: no JSON tokens reach it, only the
    /// `ColumnDef` / `RowDef` intermediates, which live here for the same reason.
    /// </remarks>
    public partial class DataTableJsonConverter
    {
        #region Build DataTable

        private static DataTable BuildDataTable(string tableName, List<ColumnDef> columns, List<string> primaryKeys, List<RowDef> rows)
        {
            var dt = new DataTable(tableName);

            // Build columns
            foreach (var col in columns)
            {
                var netType = DbTypeConverter.ToType(col.FieldType);
                var dc = new DataColumn(col.Name, netType)
                {
                    AllowDBNull = col.AllowNull,
                    ReadOnly = col.ReadOnly,
                    MaxLength = col.MaxLength,
                    Caption = col.Caption,
                    DefaultValue = col.DefaultValue ?? DBNull.Value
                };
                // The .NET default for a fresh DateTime column is `UnspecifiedLocal`, the one mode
                // that writes a time-zone offset into XML. Match what the framework's own
                // `AddColumn` produces so a rebuilt table cannot shift when persisted as XML.
                if (netType == typeof(DateTime)) { dc.DateTimeMode = DataSetDateTime.Unspecified; }
                // Several FieldDbType values share one CLR type, so the wire value carries information
                // the rebuilt DataColumn.DataType cannot. Record it so the client side stays as
                // self-describing as the payload was.
                dc.ApplyFieldDbType(col.FieldType);
                dt.Columns.Add(dc);
            }

            // Set primary keys
            if (primaryKeys.Count > 0)
            {
                var pkCols = primaryKeys
                    .Where(pk => dt.Columns.Contains(pk))
                    .Select(pk => dt.Columns[pk]!)
                    .ToArray();
                if (pkCols.Length > 0)
                    dt.PrimaryKey = pkCols;
            }

            // Restore rows (same logic as SerializableDataTable.ToDataTable)
            foreach (var rowDef in rows)
            {
                var row = dt.NewRow();

                switch (rowDef.State)
                {
                    case DataRowState.Unchanged:
                        SetRowValues(row, rowDef.CurrentValues);
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        break;

                    case DataRowState.Added:
                        SetRowValues(row, rowDef.CurrentValues);
                        dt.Rows.Add(row);
                        break;

                    case DataRowState.Modified:
                        // Write original values first
                        SetRowValues(row, rowDef.OriginalValues);
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        // Overwrite with current values
                        SetRowValues(row, rowDef.CurrentValues);
                        break;

                    case DataRowState.Deleted:
                        SetRowValues(row, rowDef.OriginalValues);
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        row.Delete();
                        break;
                }
            }

            return dt;
        }

        private static void SetRowValues(DataRow row, Dictionary<string, object?>? values)
        {
            if (values == null) return;
            foreach (var kvp in values)
            {
                row[kvp.Key] = kvp.Value ?? DBNull.Value;
            }
        }

        #endregion

        #region Internal DTOs

        private sealed class ColumnDef
        {
            public string Name { get; set; } = string.Empty;
            public FieldDbType FieldType { get; set; } = FieldDbType.String;
            public bool AllowNull { get; set; } = true;
            public bool ReadOnly { get; set; }
            public int MaxLength { get; set; } = -1;
            public string Caption { get; set; } = string.Empty;
            public object? DefaultValue { get; set; }
        }

        private sealed class RowDef
        {
            public DataRowState State { get; set; } = DataRowState.Added;
            public Dictionary<string, object?>? CurrentValues { get; set; }
            public Dictionary<string, object?>? OriginalValues { get; set; }
        }

        #endregion
    }
}
