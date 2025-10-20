using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 表示單一排序項目。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public sealed class SortItem : MessagePackCollectionItem
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public SortItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="field">欄位名稱。</param>
        /// <param name="direction">排序方向。</param>
        public SortItem(string field, SortDirection direction)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("Field cannot be null or empty.", nameof(field));
            }

            Field = field;
            Direction = direction;
        }

        /// <summary>
        /// 欄位名稱或 SQL 運算式。
        /// </summary>
        [Key(100)]
        public string Field { get; set; }

        /// <summary>
        /// 排序方向。
        /// </summary>
        [Key(101)]
        public SortDirection Direction { get; set; }
    }
}
