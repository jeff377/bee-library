using Bee.Base;
using Bee.Base.Collections;

namespace Bee.Db.Dml
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
            return this.FirstOrDefault(item => StringUtilities.IsEquals(item.RightAlias, rightAlias));
        }
    }
}
