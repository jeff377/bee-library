using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Db
{
    /// <summary>
    /// 執行批次命令的輸出結果。
    /// </summary>
    public class DbBatchResult
    {
        /// <summary>
        /// 每一個命令的結果（依輸入順序一一對應）。
        /// </summary>
        public DbCommandResultCollection Results { get; set; } = new DbCommandResultCollection();

        /// <summary>
        /// 受影響筆數加總（僅針對 NonQuery 類型累加）。
        /// </summary>
        public int RowsAffectedSum { get; set; }
    }
}
