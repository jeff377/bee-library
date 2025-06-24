using System;
using System.Data;
using Bee.Base;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 清單項目集合。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ListItemCollection : KeyCollectionBase<ListItem>
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ListItemCollection()
        { }

        #endregion

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="value">項目值。</param>
        /// <param name="text">顯示文字。</param>
        public ListItem Add(string value, string text)
        {
            var item = new ListItem(value, text);
            this.Add(item);
            return item;
        }

        /// <summary>
        /// 由資料表產生成員。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="valueField">項目值對應欄位名稱。</param>
        /// <param name="textField">顯示文字對應欄位名稱。</param>
        public void FromTable(DataTable table, string valueField, string textField)
        {
            foreach (DataRow row in table.Rows)
            {
                Add(BaseFunc.CStr(row[valueField]), BaseFunc.CStr(row[textField]));
            }
        }
    }
}
