using Bee.Base;
using Bee.Base.Collections;

namespace Bee.Db.Query
{
    /// <summary>
    /// A collection of <see cref="TableJoin"/> instances describing JOIN relationships between tables.
    /// </summary>
    public class TableJoinCollection : KeyCollectionBase<TableJoin>
    {
        /// <summary>
        /// Finds a JOIN relationship by the right-side table alias.
        /// </summary>
        /// <param name="rightAlias">The right-side table alias.</param>
        public TableJoin? FindRightAlias(string rightAlias)
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
