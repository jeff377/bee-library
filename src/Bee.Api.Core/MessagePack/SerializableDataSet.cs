using System.Collections.Generic;
using System.Data;
using System.Linq;
using MessagePack;

namespace Bee.Api.Core
{
    /// <summary>
    /// 可序列化的 DataSet 物件，用於支援跨平台傳輸與儲存。
    /// </summary>
    [MessagePackObject]
    public class SerializableDataSet
    {
        /// <summary>
        /// 資料集名稱。
        /// </summary>
        [Key(0)]
        public string DataSetName { get; set; }

        /// <summary>
        /// 所有的資料表集合。
        /// </summary>
        [Key(1)]
        public List<SerializableDataTable> Tables { get; set; }

        /// <summary>
        /// 資料表之間的關聯定義集合。
        /// </summary>
        [Key(2)]
        public List<SerializableDataRelation> Relations { get; set; }

        /// <summary>
        /// 建構函式，初始化集合。
        /// </summary>
        public SerializableDataSet()
        {
            Tables = new List<SerializableDataTable>();
            Relations = new List<SerializableDataRelation>();
        }

        /// <summary>
        /// 將標準 DataSet 轉換為可序列化物件。
        /// </summary>
        /// <param name="ds">來源 DataSet。</param>
        /// <returns>可序列化的資料集物件。</returns>
        public static SerializableDataSet FromDataSet(DataSet ds)
        {
            var sds = new SerializableDataSet
            {
                DataSetName = ds.DataSetName
            };

            foreach (DataTable table in ds.Tables)
                sds.Tables.Add(SerializableDataTable.FromDataTable(table));

            foreach (DataRelation rel in ds.Relations)
            {
                sds.Relations.Add(new SerializableDataRelation
                {
                    RelationName = rel.RelationName,
                    ParentTable = rel.ParentTable.TableName,
                    ChildTable = rel.ChildTable.TableName,
                    ParentColumns = rel.ParentColumns.Select(c => c.ColumnName).ToList(),
                    ChildColumns = rel.ChildColumns.Select(c => c.ColumnName).ToList()
                });
            }

            return sds;
        }

        /// <summary>
        /// 將可序列化資料集還原為標準 DataSet。
        /// </summary>
        /// <param name="sds">可序列化的資料集。</param>
        /// <returns>還原後的標準 DataSet。</returns>
        public static DataSet ToDataSet(SerializableDataSet sds)
        {
            var ds = new DataSet(sds.DataSetName);

            foreach (var table in sds.Tables)
                ds.Tables.Add(SerializableDataTable.ToDataTable(table));

            foreach (var rel in sds.Relations)
            {
                var parentCols = rel.ParentColumns.Select(c => ds.Tables[rel.ParentTable].Columns[c]).ToArray();
                var childCols = rel.ChildColumns.Select(c => ds.Tables[rel.ChildTable].Columns[c]).ToArray();
                ds.Relations.Add(new DataRelation(rel.RelationName, parentCols, childCols));
            }

            return ds;
        }
    }

}
