using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 表單資料表集合。
    /// </summary>
    [Serializable]
    [Description("表單資料表集合。")]
    [TreeNode("資料表", false)]
    public class FormTableCollection : KeyCollectionBase<FormTable>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public FormTableCollection(FormDefine formDefine) : base(formDefine)
        { }


        /// <summary>
        /// 加入資料表。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="displayName">顯示名稱。</param>
        public FormTable Add(string tableName, string displayName)
        {
            var table = new FormTable(tableName, displayName);
            base.Add(table);
            return table;
        }
    }
}
