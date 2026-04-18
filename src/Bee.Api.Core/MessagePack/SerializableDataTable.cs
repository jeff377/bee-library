using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bee.Base.Data;
using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializable DataTable object used to transport DataTable structure and data.
    /// </summary>
    [MessagePackObject]
    public class SerializableDataTable
    {
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        [Key(0)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the column definitions for the table.
        /// </summary>
        [Key(1)]
        public List<SerializableDataColumn> Columns { get; set; }

        /// <summary>
        /// Gets or sets the row data collection.
        /// </summary>
        [Key(2)]
        public List<SerializableDataRow> Rows { get; set; }

        /// <summary>
        /// Gets or sets the primary key column names.
        /// </summary>
        [Key(3)]
        public List<string> PrimaryKeys { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDataTable"/> class and initializes the collections.
        /// </summary>
        public SerializableDataTable()
        {
            Columns = new List<SerializableDataColumn>();
            Rows = new List<SerializableDataRow>();
            PrimaryKeys = new List<string>();
        }

        /// <summary>
        /// Converts a DataTable to a serializable format.
        /// </summary>
        /// <param name="table">The source DataTable.</param>
        /// <returns>The serializable DataTable.</returns>
        public static SerializableDataTable FromDataTable(DataTable table)
        {
            var sdt = new SerializableDataTable
            {
                TableName = table.TableName
            };

            foreach (DataColumn col in table.Columns)
                sdt.Columns.Add(BuildSerializableColumn(col));

            sdt.PrimaryKeys.AddRange(table.PrimaryKey.Select(pk => pk.ColumnName));

            foreach (DataRow row in table.Rows)
            {
                var srow = BuildSerializableRow(row, table.Columns);
                if (srow != null) sdt.Rows.Add(srow);
            }

            return sdt;
        }

        private static SerializableDataColumn BuildSerializableColumn(DataColumn col)
        {
            return new SerializableDataColumn
            {
                ColumnName = col.ColumnName,
                DataType = DbTypeConverter.ToFieldDbType(col.DataType),
                DisplayName = col.Caption,
                AllowDBNull = col.AllowDBNull,
                ReadOnly = col.ReadOnly,
                MaxLength = col.MaxLength,
                DefaultValue = col.DefaultValue is DBNull ? null : col.DefaultValue
            };
        }

        private static SerializableDataRow? BuildSerializableRow(DataRow row, DataColumnCollection columns)
        {
            var state = row.RowState;
            Dictionary<string, object?>? current = null;
            Dictionary<string, object?>? original = null;

            switch (state)
            {
                case DataRowState.Added:
                    current = CopyRowValues(row, columns, DataRowVersion.Current);
                    break;
                case DataRowState.Deleted:
                    original = CopyRowValues(row, columns, DataRowVersion.Original);
                    break;
                case DataRowState.Modified:
                case DataRowState.Unchanged:
                    current = CopyRowValues(row, columns, DataRowVersion.Current);
                    original = CopyRowValues(row, columns, DataRowVersion.Original);
                    break;
                default:
                    // Skip Detached or other states
                    return null;
            }

            return new SerializableDataRow
            {
                CurrentValues = current != null && current.Count > 0 ? current : null,
                OriginalValues = original != null && original.Count > 0 ? original : null,
                RowState = state
            };
        }

        private static Dictionary<string, object?> CopyRowValues(DataRow row, DataColumnCollection columns, DataRowVersion version)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in columns)
            {
                var val = row[col, version];
                dict[col.ColumnName] = val is DBNull ? null : val;
            }
            return dict;
        }

        /// <summary>
        /// Restores a serializable DataTable back to a standard DataTable.
        /// </summary>
        /// <param name="sdt">The serializable DataTable.</param>
        /// <returns>The standard DataTable.</returns>
        public static DataTable ToDataTable(SerializableDataTable sdt)
        {
            var dt = new DataTable(sdt.TableName);

            BuildColumns(dt, sdt.Columns);
            RestorePrimaryKeys(dt, sdt.PrimaryKeys);

            foreach (var srow in sdt.Rows)
                RestoreRow(dt, srow);

            return dt;
        }

        private static void BuildColumns(DataTable dt, List<SerializableDataColumn> columns)
        {
            foreach (var col in columns)
            {
                var type = DbTypeConverter.ToType(col.DataType);
                var dc = new DataColumn(col.ColumnName, type)
                {
                    Caption = col.DisplayName,
                    AllowDBNull = col.AllowDBNull,
                    ReadOnly = col.ReadOnly,
                    MaxLength = col.MaxLength,
                    DefaultValue = col.DefaultValue ?? DBNull.Value
                };
                dt.Columns.Add(dc);
            }
        }

        private static void RestorePrimaryKeys(DataTable dt, List<string> primaryKeys)
        {
            if (primaryKeys.Count == 0) return;

            var primaryCols = primaryKeys
                .Select(pk => dt.Columns.Contains(pk) ? dt.Columns[pk] : null)
                .Where(c => c != null)
                .Select(c => c!)
                .ToArray();

            if (primaryCols.Length > 0)
                dt.PrimaryKey = primaryCols;
        }

        private static void RestoreRow(DataTable dt, SerializableDataRow srow)
        {
            DataRow row = dt.NewRow();

            switch (srow.RowState)
            {
                case DataRowState.Unchanged:
                    ApplyValues(row, srow.CurrentValues!);
                    dt.Rows.Add(row);
                    row.AcceptChanges();
                    break;

                case DataRowState.Added:
                    ApplyValues(row, srow.CurrentValues!);
                    dt.Rows.Add(row);
                    break;

                case DataRowState.Modified:
                    ApplyValues(row, srow.OriginalValues!);
                    dt.Rows.Add(row);
                    row.AcceptChanges();
                    ApplyValues(row, srow.CurrentValues!);
                    break;

                case DataRowState.Deleted:
                    ApplyValues(row, srow.OriginalValues!);
                    dt.Rows.Add(row);
                    row.AcceptChanges();
                    row.Delete();
                    break;
            }
        }

        private static void ApplyValues(DataRow row, Dictionary<string, object?> values)
        {
            foreach (var kvp in values)
                row[kvp.Key] = kvp.Value ?? DBNull.Value;
        }

    }
}
