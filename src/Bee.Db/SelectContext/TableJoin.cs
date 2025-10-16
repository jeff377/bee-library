using System.Collections.Generic;
using Bee.Base;

namespace Bee.Db
{
    /// <summary>
    /// 描述兩個資料表之間的 Join 關係。
    /// </summary>
    public class TableJoin : KeyCollectionItem
    {
        /// <summary>
        /// 鍵值，記錄建立 Join 關係的參考來源的唯一鍵值。
        /// </summary>
        public override string Key
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Join 類型。
        /// </summary>
        public JoinType JoinType { get; set; } = JoinType.Left;

        /// <summary>
        /// 左側資料表名稱。
        /// </summary>
        public string LeftTable { get; set; }

        /// <summary>
        /// 左側資料表別名。
        /// </summary>
        public string LeftAlias { get; set; }

        /// <summary>
        /// 左側欄位名稱。
        /// </summary>
        public string LeftField { get; set; }

        /// <summary>
        /// 右側資料表名稱。
        /// </summary>
        public string RightTable { get; set; }

        /// <summary>
        /// 右側資料表別名。
        /// </summary>
        public string RightAlias { get; set; }

        /// <summary>
        /// 右側欄位名稱。
        /// </summary>
        public string RightField { get; set; }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            var joinKeyword = JoinType.ToString().ToUpperInvariant() + " JOIN";
            return $"{joinKeyword} {RightTable} {RightAlias} ON {LeftAlias}.{LeftField} = {RightAlias}.{RightField}";
        }
    }

}
