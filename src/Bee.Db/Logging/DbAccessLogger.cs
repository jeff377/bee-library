using Bee.Define;
using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Bee.Db
{
    /// <summary>
    /// 提供資料庫存取的日誌記錄功能。
    /// 包含命令執行起始、結束、異常與錯誤的記錄，
    /// 可用於追蹤 SQL 執行效能、異常行為與錯誤分析。
    /// </summary>
    public static class DbAccessLogger
    {
        /// <summary>
        /// 開始記錄。
        /// </summary>
        /// <param name="command">資料庫命令描述。</param>
        /// <param name="databaseId">資料庫識別。</param>
        public static DbLogContext LogStart(DbCommandSpec command, string databaseId = "")
        {
            return new DbLogContext(command.CommandText, databaseId: databaseId);
        }

        /// <summary>
        /// 結束記錄。
        /// 停止計時，並根據設定判斷是否需記錄慢查詢或大量異動的警告。
        /// </summary>
        /// <param name="context">資料庫存取日誌的上下文資訊。</param>
        /// <param name="affectedRows">受影響的資料列數，預設為 -1 表示未知。</param>
        public static void LogEnd(DbLogContext context, int affectedRows = -1)
        {
            if (context == null) throw new ArgumentNullException(nameof(context), "context cannot be null.");
            context.Stopwatch.Stop();

            var opts = BackendInfo.LogOptions == null ? null : BackendInfo.LogOptions.DbAccess;
            if (opts == null) return;

            var elapsedSeconds = context.Stopwatch.Elapsed.TotalSeconds;
            bool isSlow = (opts.ExecutionTimeThreshold > 0) && (elapsedSeconds >= opts.ExecutionTimeThreshold);
            bool isLarge = (opts.AffectedRowThreshold > 0) && (affectedRows >= opts.AffectedRowThreshold);

            if (opts.Level == DbAccessAnomalyLogLevel.Warning && (isSlow || isLarge))
            {
                WriteWarning(context, affectedRows, elapsedSeconds, isSlow, isLarge);
            }
        }

        /// <summary>
        /// 記錄資料庫存取錯誤。
        /// 停止計時器，並記錄例外資訊、執行時間、資料庫識別與 SQL 命令內容。
        /// </summary>
        /// <param name="context">資料庫存取日誌的上下文資訊。</param>
        /// <param name="exception">發生的例外物件。</param>
        public static void LogError(DbLogContext context, Exception exception)
        {
            if (context == null) throw new ArgumentNullException(nameof(context), "context cannot be null.");
            if (exception == null) throw new ArgumentNullException(nameof(exception), "exception cannot be null.");

            try { context.Stopwatch.Stop(); } catch { /* ignore */ }

            var elapsedSeconds = context.Stopwatch.Elapsed.TotalSeconds;
            var (errCode, errNumber) = GetDbErrorInfo(exception);

            var sb = new StringBuilder(300);
            sb.Append("SQL execution error. ");
            if (!string.IsNullOrEmpty(context.DatabaseId)) sb.Append("DatabaseId=").Append(context.DatabaseId).Append("; ");
            sb.Append("Elapsed=").Append(elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(" s; ");
            if (errCode.HasValue) sb.Append("ErrorCode=").Append(errCode.Value).Append("; ");
            if (errNumber.HasValue) sb.Append("Number=").Append(errNumber.Value).Append("; ");
            sb.Append("Exception=").Append(exception.GetType().FullName).Append("; ");
            sb.Append("Message=").Append(exception.Message).Append("; ");
            sb.Append("CommandText=").Append(context.CommandText);

            // 寫入錯誤記錄
            // SysInfo.LogWriter?.WriteError(sb.ToString());
        }

        /// <summary>
        /// 寫入異常警告日誌。
        /// </summary>
        /// <param name="ctx">資料庫存取日誌的上下文資訊。</param>
        /// <param name="affectedRows">受影響的資料列數。</param>
        /// <param name="elapsedSeconds">執行所花費的秒數。</param>
        /// <param name="isSlow">是否為慢查詢。</param>
        /// <param name="isLarge">是否為大量異動。</param>
        private static void WriteWarning(DbLogContext ctx, int affectedRows, double elapsedSeconds, bool isSlow, bool isLarge)
        {
            var sb = new StringBuilder(300);
            sb.Append("SQL anomaly detected (");
            if (isSlow) sb.Append("Slow");
            if (isSlow && isLarge) sb.Append(", ");
            if (isLarge) sb.Append("LargeUpdate");
            sb.Append("). ");

            if (!string.IsNullOrEmpty(ctx.DatabaseId)) sb.Append("DbId=").Append(ctx.DatabaseId).Append("; ");
            sb.Append("Elapsed=").Append(elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(" s; ");
            if (affectedRows >= 0) sb.Append("Rows=").Append(affectedRows).Append("; ");
            sb.Append("CommandText=").Append(ctx.CommandText);

            // TODO : 寫入錯誤記錄
            // SysInfo.LogWriter?.WriteError(sb.ToString());
        }

        /// <summary>
        /// 取得資料庫例外的錯誤代碼與錯誤編號（如有）。
        /// </summary>
        /// <param name="ex">例外物件。</param>
        /// <returns>Tuple：Item1 為 ErrorCode，Item2 為 Number（如有）。</returns>
        private static Tuple<int?, int?> GetDbErrorInfo(Exception ex)
        {
            int? errorCode = null;
            if (ex is System.Data.Common.DbException dbex) errorCode = dbex.ErrorCode;

            int? number = null;
            var prop = ex.GetType().GetProperty("Number", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(int))
            {
                try { number = (int)prop.GetValue(ex, null); } catch { /* ignore */ }
            }
            return Tuple.Create(errorCode, number);
        }
    }
}
