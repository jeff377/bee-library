using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 可序列化的資料表物件，用於傳輸 DataTable 結構與資料。
    /// </summary>
    [MessagePackObject]
    public class TSerializableDataTable
    {
        /// <summary>
        /// 資料表名稱。
        /// </summary>
        [Key(0)]
        public string TableName { get; set; }

        /// <summary>
        /// 資料表的欄位集合。
        /// </summary>
        [Key(1)]
        public List<TSerializableDataColumn> Columns { get; set; }

        /// <summary>
        /// 資料列資料集合。
        /// </summary>
        [Key(2)]
        public List<TSerializableDataRow> Rows { get; set; }

        /// <summary>
        /// 主索引鍵欄位名稱集合。
        /// </summary>
        [Key(3)]
        public List<string> PrimaryKeys { get; set; }

        /// <summary>
        /// 建構函式，初始化集合。
        /// </summary>
        public TSerializableDataTable()
        {
            Columns = new List<TSerializableDataColumn>();
            Rows = new List<TSerializableDataRow>();
            PrimaryKeys = new List<string>();
        }

        /// <summary>
        /// 將 DataTable 轉換為可序列化格式。
        /// </summary>
        /// <param name="table">來源 DataTable。</param>
        /// <returns>可序列化資料表。</returns>
        public static TSerializableDataTable FromDataTable(DataTable table)
        {
            var sdt = new TSerializableDataTable
            {
                TableName = table.TableName
            };

            foreach (DataColumn col in table.Columns)
            {
                sdt.Columns.Add(new TSerializableDataColumn
                {
                    ColumnName = col.ColumnName,
                    DataType = col.DataType.AssemblyQualifiedName,
                    DisplayName = col.Caption,
                    AllowDBNull = col.AllowDBNull,
                    ReadOnly = col.ReadOnly,
                    MaxLength = col.MaxLength,
                    DefaultValue = col.DefaultValue is DBNull ? null : col.DefaultValue
                });
            }

            sdt.PrimaryKeys.AddRange(table.PrimaryKey.Select(pk => pk.ColumnName));

            foreach (DataRow row in table.Rows)
            {
                var current = new Dictionary<string, object>();
                var original = new Dictionary<string, object>();

                foreach (DataColumn col in table.Columns)
                {
                    object currentValue = row[col] is DBNull ? null : row[col];
                    current[col.ColumnName] = currentValue;

                    if (row.RowState != DataRowState.Added)
                    {
                        object originalValue = row[col, DataRowVersion.Original] is DBNull ? null : row[col, DataRowVersion.Original];
                        original[col.ColumnName] = originalValue;
                    }
                }

                sdt.Rows.Add(new TSerializableDataRow
                {
                    CurrentValues = current,
                    OriginalValues = original,
                    RowState = row.RowState
                });
            }

            return sdt;
        }

        /// <summary>
        /// 將可序列化資料表還原為標準 DataTable。
        /// </summary>
        /// <param name="sdt">可序列化資料表。</param>
        /// <returns>標準 DataTable。</returns>
        public static DataTable ToDataTable(TSerializableDataTable sdt)
        {
            var dt = new DataTable(sdt.TableName);

            foreach (var col in sdt.Columns)
            {
                var type = Type.GetType(col.DataType) ?? typeof(string);
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

            if (sdt.PrimaryKeys.Count > 0)
            {
                var primaryCols = sdt.PrimaryKeys
                    .Select(pk => dt.Columns.Contains(pk) ? dt.Columns[pk] : null)
                    .Where(col => col != null)
                    .ToArray();

                if (primaryCols.Length > 0)
                    dt.PrimaryKey = primaryCols;
            }

            foreach (var srow in sdt.Rows)
            {
                var row = dt.NewRow();

                foreach (var kvp in srow.CurrentValues)
                    row[kvp.Key] = kvp.Value ?? DBNull.Value;

                dt.Rows.Add(row);

                if (srow.RowState == DataRowState.Unchanged)
                {
                    row.AcceptChanges();
                }
                else if (srow.RowState == DataRowState.Modified)
                {
                    row.AcceptChanges();
                    foreach (var kvp in srow.OriginalValues)
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    row.SetModified();
                }
                else if (srow.RowState == DataRowState.Deleted)
                {
                    row.AcceptChanges();
                    foreach (var kvp in srow.OriginalValues)
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    row.Delete();
                }
            }

            return dt;
        }
    }
}
