using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 排序欄位。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public sealed class SortField : MessagePackCollectionItem
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public SortField()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="direction">排序方向。</param>
        public SortField(string fieldName, SortDirection direction)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field cannot be null or empty.", nameof(fieldName));

            FieldName = fieldName;
            Direction = direction;
        }

        /// <summary>
        /// 欄位名稱或 SQL 運算式。
        /// </summary>
        [Key(100)]
        public string FieldName { get; set; }

        /// <summary>
        /// 排序方向。
        /// </summary>
        [Key(101)]
        public SortDirection Direction { get; set; }
    }
}
