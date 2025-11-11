using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料表索引集合。
    /// </summary>
    [TreeNode("索引", true)]
    [Serializable]
    public class DbTableIndexCollection : KeyCollectionBase<DbTableIndex>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        public DbTableIndexCollection(DbTable dbTable) : base(dbTable)
        { }

        /// <summary>
        /// 加入主索引。
        /// </summary>
        /// <param name="fields">欄位名稱集合字串，以逗點分隔。</param>
        public DbTableIndex AddPrimaryKey(string fields)
        {
            var index = new DbTableIndex()
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
        public DbTableIndex Add(string name, string fields, bool unique)
        {
            DbTableIndex oIndex;
            string[] oFields;

            oIndex = new DbTableIndex();
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
