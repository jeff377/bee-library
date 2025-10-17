using Bee.Base;

namespace Bee.Db
{
    /// <summary>
    /// 描述兩個資料表之間的 Join 關係的集合。
    /// </summary>
    public class TableJoinCollection : KeyCollectionBase<TableJoin>
    {
        /// <summary>
        /// 依據右側資料表別名尋找 Join 關係。
        /// </summary>
        /// <param name="rightAlias">右側資料表別名。</param>
        public TableJoin FindRightAlias(string rightAlias)
        {
            if (string.IsNullOrEmpty(rightAlias))
                return null;
            foreach (var item in this)
            {
                if (StrFunc.Equals(item.RightAlias, rightAlias))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
