using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Bee.Base;

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
        /// </summary>
        public static void LogEnd(DbLogContext context, int affectedRows = -1)
        {
            if (context == null) throw new ArgumentNullException(nameof(context), "context cannot be null.");
            context.Stopwatch.Stop();

            var opts = SysInfo.LogOptions == null ? null : SysInfo.LogOptions.DbAccess;
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
        /// 記錄錯誤。
        /// </summary>
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
        /// 記錄異常。
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="affectedRows"></param>
        /// <param name="elapsedSeconds"></param>
        /// <param name="isSlow"></param>
        /// <param name="isLarge"></param>
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
