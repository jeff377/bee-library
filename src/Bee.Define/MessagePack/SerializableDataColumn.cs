using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 可序列化的資料欄位定義，用於描述 DataColumn 屬性。
    /// </summary>
    [MessagePackObject]
    public class SerializableDataColumn
    {
        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [Key(0)]
        public string ColumnName { get; set; }

        /// <summary>
        /// 資料型別（AssemblyQualifiedName）。
        /// </summary>
        [Key(1)]
        public string DataType { get; set; }

        /// <summary>
        /// 顯示名稱（Caption）。
        /// </summary>
        [Key(2)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 是否允許 NULL 值。
        /// </summary>
        [Key(3)]
        public bool AllowDBNull { get; set; }

        /// <summary>
        /// 是否為唯讀欄位。
        /// </summary>
        [Key(4)]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// 最大長度（僅對字串型別有效）。
        /// </summary>
        [Key(5)]
        public int MaxLength { get; set; }

        /// <summary>
        /// 預設值。
        /// </summary>
        [Key(6)]
        public object DefaultValue { get; set; }
    }

}
