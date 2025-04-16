using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料表索引集合。
    /// </summary>
    [TreeNode("索引", true)]
    [Serializable]
    public class TDbTableIndexCollection : TKeyCollectionBase<TDbTableIndex>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        public TDbTableIndexCollection(TDbTable dbTable) : base(dbTable)
        { }

        /// <summary>
        /// 加入主索引。
        /// </summary>
        /// <param name="fields">欄位名稱集合字串，以逗點分隔。</param>
        public TDbTableIndex AddPrimaryKey(string fields)
        {
            TDbTableIndex oIndex;
            string[] oFields;

            oIndex = new TDbTableIndex();
            oIndex.Name = "PK";
            oIndex.Unique = true;
            oIndex.PrimaryKey = true;
            oFields = StrFunc.Split(fields, ",");
            foreach (string fieldName in oFields)
                oIndex.IndexFields.Add(fieldName);
            this.Add(oIndex);
            return oIndex;
        }

        /// <summary>
        /// 加入索引。
        /// </summary>
        /// <param name="name">索引名稱。</param>
        /// <param name="fields">欄位名稱集合字串，以逗點分隔。</param>
        /// <param name="unique">是否具有唯一性。</param>
        public TDbTableIndex Add(string name, string fields, bool unique)
        {
            TDbTableIndex oIndex;
            string[] oFields;

            oIndex = new TDbTableIndex();
            oIndex.Name = name;
            oIndex.Unique = unique;
            oFields = StrFunc.Split(fields, ",");
            foreach (string fieldName in oFields)
                oIndex.IndexFields.Add(fieldName);
            this.Add(oIndex);
            return oIndex;
        }
    }
}
