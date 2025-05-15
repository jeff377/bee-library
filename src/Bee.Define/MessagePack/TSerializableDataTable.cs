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

            // 處理欄位定義
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

            // 主鍵欄位
            sdt.PrimaryKeys.AddRange(table.PrimaryKey.Select(pk => pk.ColumnName));

            // 處理資料列
            foreach (DataRow row in table.Rows)
            {
                var current = new Dictionary<string, object>();
                var original = new Dictionary<string, object>();
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
                        // Detached 或其他狀態略過或紀錄警告（可視情況擴充）
                        continue;
                }

                sdt.Rows.Add(new TSerializableDataRow
                {
                    CurrentValues = current.Count > 0 ? current : null,
                    OriginalValues = original.Count > 0 ? original : null,
                    RowState = state
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

            // 建立欄位結構
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

            // 設定主鍵
            if (sdt.PrimaryKeys.Count > 0)
            {
                var primaryCols = sdt.PrimaryKeys
                    .Select(pk => dt.Columns.Contains(pk) ? dt.Columns[pk] : null)
                    .Where(c => c != null)
                    .ToArray();

                if (primaryCols.Length > 0)
                    dt.PrimaryKey = primaryCols;
            }

            // 逐筆還原
            foreach (var srow in sdt.Rows)
            {
                DataRow row = dt.NewRow();

                switch (srow.RowState)
                {
                    case DataRowState.Unchanged:
                        foreach (var kvp in srow.CurrentValues)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        break;

                    case DataRowState.Added:
                        foreach (var kvp in srow.CurrentValues)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        dt.Rows.Add(row);
                        // 不呼叫 AcceptChanges，保持 Added 狀態
                        break;

                    case DataRowState.Modified:
                        // 先寫入修改前的值
                        foreach (var kvp in srow.OriginalValues)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        dt.Rows.Add(row);

                        // 接受變更，狀態為 Unchanged
                        row.AcceptChanges();

                        // 再寫入修改後的值，會自動標記為 Modified
                        foreach (var kvp in srow.CurrentValues)
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                        break;

                    case DataRowState.Deleted:
                        // 用原始值建立列，加入，AcceptChanges，然後刪除
                        foreach (var kvp in srow.OriginalValues)
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
