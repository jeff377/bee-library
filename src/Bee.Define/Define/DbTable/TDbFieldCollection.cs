using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 結構欄位集合。
    /// </summary>
    [TreeNode("欄位", true)]
    [Serializable]
    public class TDbFieldCollection : TKeyCollectionBase<TDbField>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        public TDbFieldCollection(TDbTable dbTable)
          : base(dbTable)
        { }

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="length">字串型別的欄位長度。</param>
        public TDbField Add(string fieldName, string caption, EFieldDbType dbType, int length = 0)
        {
            TDbField oItem;

            oItem = new TDbField(fieldName, caption, dbType);
            oItem.Length = length;  
            this.Add(oItem);
            return oItem;
        }
    }
}
