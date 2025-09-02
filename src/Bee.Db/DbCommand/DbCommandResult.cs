using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// DbCommand 執行結果的統一包裝。
    /// </summary>
    public class DbCommandResult
    {
        /// <summary>
        /// 受影響筆數（NonQuery）。
        /// </summary>
        public int? RowsAffected { get; set; }

        /// <summary>
        /// 純量結果（Scalar）。
        /// </summary>
        public object Scalar { get; set; }

        /// <summary>
        /// 單一資料表。
        /// </summary>
        public DataTable Table { get; set; }
    }
}
