using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// DbCommand 執行結果的統一包裝。
    /// </summary>
    public class DbCommandResult
    {
        /// <summary>
        /// 資料庫命令的執行種類。
        /// </summary>
        public DbCommandKind Kind { get; private set; }

        /// <summary>
        /// 受影響筆數（NonQuery）。
        /// </summary>
        public int? RowsAffected { get; private set; }

        /// <summary>
        /// 純量結果（Scalar）。
        /// </summary>
        public object Scalar { get; private set; }

        /// <summary>
        /// 單一資料表。
        /// </summary>
        public DataTable Table { get; private set; }

        /// <summary>
        /// 建立 NonQuery 結果包裝。
        /// </summary>
        /// <param name="rows">受影響筆數。</param>
        public static DbCommandResult ForRowsAffected(int rows)
            => new DbCommandResult { Kind = DbCommandKind.NonQuery, RowsAffected = rows };

        /// <summary>
        /// 建立 Scalar 結果包裝。
        /// </summary>
        /// <param name="value">純量結果。</param>
        public static DbCommandResult ForScalar(object value)
            => new DbCommandResult { Kind = DbCommandKind.Scalar, Scalar = value };

        /// <summary>
        /// 建立 DataTable 結果包裝。
        /// </summary>
        /// <param name="table">資料表。</param>
        public static DbCommandResult ForTable(DataTable table)
            => new DbCommandResult { Kind = DbCommandKind.DataTable, Table = table };
    }
}
