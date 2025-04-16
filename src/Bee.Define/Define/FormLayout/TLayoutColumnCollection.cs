using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 排版資料表格欄位集合。
    /// </summary>
    [Serializable]
    [Description("排版資料表格欄位集合。")]
    [TreeNode("欄位", false)]
    public class TLayoutColumnCollection : TCollectionBase<TLayoutColumn>
    {
        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="controlType">控制項類型。</param>
        public TLayoutColumn Add(string fieldName, string caption, EColumnControlType controlType)
        {
            TLayoutColumn oColumn;

            oColumn = new TLayoutColumn(fieldName, caption, controlType);
            this.Add(oColumn);
            return oColumn;
        }
    }
}
