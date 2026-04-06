using System;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define.Database
{
    /// <summary>
    /// 資料表索引集合。
    /// </summary>
    [TreeNode("索引", true)]
    [Serializable]
    public class TableSchemaIndexCollection : KeyCollectionBase<TableSchemaIndex>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="tableSchema">資料表結構。</param>
        public TableSchemaIndexCollection(TableSchema tableSchema) : base(tableSchema)
        { }

        /// <summary>
        /// 加入主索引。
        /// </summary>
        /// <param name="fields">欄位名稱集合字串，以逗點分隔。</param>
        public TableSchemaIndex AddPrimaryKey(string fields)
        {
            var index = new TableSchemaIndex()
            {
                Name = "pk_{0}",
                Unique = true,
                PrimaryKey = true
            };

            string[] fieldNames = StrFunc.Split(fields, ",");
            foreach (string fieldName in fieldNames)
                index.IndexFields.Add(fieldName);
            Add(index);
            return index;
        }

        /// <summary>
        /// 加入索引。
        /// </summary>
        /// <param name="name">索引名稱。</param>
        /// <param name="fields">欄位名稱集合字串，以逗點分隔。</param>
        /// <param name="unique">是否具有唯一性。</param>
        public TableSchemaIndex Add(string name, string fields, bool unique)
        {
            TableSchemaIndex oIndex;
            string[] oFields;

            oIndex = new TableSchemaIndex();
            oIndex.Name = name;
            oIndex.Unique = unique;
            oFields = StrFunc.Split(fields, ",");
            foreach (string fieldName in oFields)
                oIndex.IndexFields.Add(fieldName);
            Add(oIndex);
            return oIndex;
        }
    }
}
