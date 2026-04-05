namespace Bee.Db
{
    /// <summary>
    /// Join 類型列舉。
    /// </summary>
    public enum JoinType
    {
        /// <summary>
        /// 內部連接（Inner Join）。
        /// </summary>
        Inner,
        /// <summary>
        /// 左外部連接（Left Outer Join）。
        /// </summary>
        Left,
        /// <summary>
        /// 右外部連接（Right Outer Join）。
        /// </summary>
        Right,
        /// <summary>
        /// 完全外部連接（Full Outer Join）。
        /// </summary>
        Full
    }
}
