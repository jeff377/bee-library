using System.Collections.Generic;
using System.Data;
using MessagePack;

namespace Bee.Api.Core
{
    /// <summary>
    /// 可序列化的資料列，包含目前值與原始值，用於支援資料狀態與修改追蹤。
    /// </summary>
    [MessagePackObject]
    public class SerializableDataRow
    {
        /// <summary>
        /// 資料列目前的值集合（欄位名稱對應值）。
        /// </summary>
        [Key(0)]
        public Dictionary<string, object> CurrentValues { get; set; }

        /// <summary>
        /// 原始值集合（適用於已修改或已刪除的資料列）。
        /// </summary>
        [Key(1)]
        public Dictionary<string, object> OriginalValues { get; set; }

        /// <summary>
        /// 資料列的狀態（Added、Modified、Deleted、Unchanged）。
        /// </summary>
        [Key(2)]
        public DataRowState RowState { get; set; }

        /// <summary>
        /// 建構函式，初始化字典。
        /// </summary>
        public SerializableDataRow()
        {
            CurrentValues = new Dictionary<string, object>();
            OriginalValues = new Dictionary<string, object>();
        }
    }

}
