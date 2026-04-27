namespace Bee.Db.Dml
{
    /// <summary>
    /// Specifies the type of SQL JOIN operation.
    /// </summary>
    public enum JoinType
    {
        /// <summary>
        /// Inner join.
        /// </summary>
        Inner,
        /// <summary>
        /// Left outer join.
        /// </summary>
        Left,
        /// <summary>
        /// Right outer join.
        /// </summary>
        Right,
        /// <summary>
        /// Full outer join.
        /// </summary>
        Full
    }
}
