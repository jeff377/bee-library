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

            // Process column definitions
            foreach (DataColumn col in table.Columns)
            {
                sdt.Columns.Add(new SerializableDataColumn
                {
                    ColumnName = col.ColumnName,
                    DataType = DbTypeConverter.ToFieldDbType(col.DataType),
                    DisplayName = col.Caption,
                    AllowDBNull = col.AllowDBNull,
                    ReadOnly = col.ReadOnly,
                    MaxLength = col.MaxLength,
                    DefaultValue = col.DefaultValue is DBNull ? null : col.DefaultValue
                });
            }

            // Primary key columns
            sdt.PrimaryKeys.AddRange(table.PrimaryKey.Select(pk => pk.ColumnName));

            // Process data rows
            foreach (DataRow row in table.Rows)
            {
                var current = new Dictionary<string, object?>();
                var original = new Dictionary<string, object?>();
                var state = row.RowState;

                switch (state)
                {
                    case DataRowState.Added:
                        foreach (DataColumn col in table.Columns)
                        {
                            current[col.ColumnName] = row[col] is DBNull ? null : row[col];
                        }
                        break;

                    case DataRowState.Deleted:
                        foreach (DataColumn col in table.Columns)
                        {
                            original[col.ColumnName] = row[col, DataRowVersion.Original] is DBNull ? null : row[col, DataRowVersion.Original];
                        }
                        break;

                    case DataRowState.Modified:
                    case DataRowState.Unchanged:
                        foreach (DataColumn col in table.Columns)
                        {
                            current[col.ColumnName] = row[col] is DBNull ? null : row[col];
                            original[col.ColumnName] = row[col, DataRowVersion.Original] is DBNull ? null : row[col, DataRowVersion.Original];
                        }
                        break;

                    default:
                        // Skip Detached or other states (can be extended as needed)
                        continue;
                }

                sdt.Rows.Add(new SerializableDataRow
                {
                    CurrentValues = current.Count > 0 ? current : null,
                    OriginalValues = original.Count > 0 ? original : null,
                    RowState = state
                });
            }

            return sdt;
        }

        /// <summary>
        /// Restores a serializable DataTable back to a standard DataTable.
        /// </summary>
        /// <param name="sdt">The serializable DataTable.</param>
        /// <returns>The standard DataTable.</returns>
        public static DataTable ToDataTable(SerializableDataTable sdt)
        {
            var dt = new DataTable(sdt.TableName);

            // Build the column structure
            foreach (var col in sdt.Columns)
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

            // Set primary keys
            if (sdt.PrimaryKeys.Count > 0)
            {
                var primaryCols = sdt.PrimaryKeys
                    .Select(pk => dt.Columns.Contains(pk) ? dt.Columns[pk] : null)
                    .Where(c => c != null)
                    .Select(c => c!)
                    .ToArray();

                if (primaryCols.Length > 0)
                    dt.PrimaryKey = primaryCols;
            }

            // Restore each row
            foreach (var srow in sdt.Rows)
            {
                DataRow row = dt.NewRow();

                switch (srow.RowState)
                {
                    case DataRowState.Unchanged:
                        foreach (var kvp in srow.CurrentValues!)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        break;

                    case DataRowState.Added:
                        foreach (var kvp in srow.CurrentValues!)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        dt.Rows.Add(row);
                        // Do not call AcceptChanges to preserve the Added state
                        break;

                    case DataRowState.Modified:
                        // Write the pre-modification (original) values first
                        foreach (var kvp in srow.OriginalValues!)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        dt.Rows.Add(row);

                        // Accept changes so the state becomes Unchanged
                        row.AcceptChanges();

                        // Write the modified values; the row will be automatically marked as Modified
                        foreach (var kvp in srow.CurrentValues!)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        break;

                    case DataRowState.Deleted:
                        // Create the row with original values, add it, accept changes, then delete it
                        foreach (var kvp in srow.OriginalValues!)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        row.Delete();
                        break;
                }
            }

            return dt;
        }

    }
}
