using System;
using System.Data;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 清單項目集合。
    /// </summary>
    [Serializable]
    public class TListItemCollection : TKeyCollectionBase<TListItem>
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TListItemCollection()
        { }

        #endregion

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="value">項目值。</param>
        /// <param name="text">顯示文字。</param>
        public TListItem Add(string value, string text)
        {
            TListItem oItem;
            oItem = new TListItem(value, text);
            this.Add(oItem);
            return oItem;
        }

        /// <summary>
        /// 由資料表產生成員。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="valueField">項目值對應欄位名稱。</param>
        /// <param name="textField">顯示文字對應欄位名稱。</param>
        public void FromTable(DataTable table, string valueField, string textField)
        {
            TListItem oItem;

            foreach (DataRow row in table.Rows)
            {
                oItem = new TListItem();
                oItem.Value = BaseFunc.CStr(row[valueField]);
                oItem.Text = BaseFunc.CStr(row[textField]);
                this.Add(oItem);
            }
        }
    }
}
