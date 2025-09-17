using System;

namespace Bee.Base
{
    #region 常數

    /// <summary>
    /// 屬性視窗的顯示分類常數。
    /// </summary>
    public class PropertyCategories
    {
        /// <summary>
        /// 行為。
        /// </summary>
        public const string Behavior = "Behavior";
        /// <summary>
        /// 資料。
        /// </summary>
        public const string Data = "Data";
        /// <summary>
        /// 外觀。
        /// </summary>
        public const string Appearance = "Appearance";
        /// <summary>
        /// 配置。
        /// </summary>
        public const string Layout = "Layout";
        /// <summary>
        /// 動作。
        /// </summary>
        public const string Action = "Action";
    }

    /// <summary>
    /// 定義常用的追蹤分類常數，供 TraceEvent.Category 使用。
    /// </summary>
    public static class TraceCategories
    {
        /// <summary>
        /// JSON-RPC 請求或回應模型。
        /// </summary>
        public const string JsonRpc = "JsonRpc";
    }

    #endregion

    #region 列舉型別

    /// <summary>
    /// 日誌事件的類型。
    /// </summary>
    public enum LogEntryType
    {
        /// <summary>
        /// 一般訊息，表示系統正常執行的資訊。
        /// </summary>
        Information,
        /// <summary>
        /// 警告訊息，表示可能的異常狀況但系統仍可繼續執行。
        /// </summary>
        Warning,
        /// <summary>
        /// 錯誤訊息，表示系統執行時發生異常或失敗。
        /// </summary>
        Error
    }

    /// <summary>
    /// Logging level for abnormal SQL executions.
    /// </summary>
    public enum DbAccessAnomalyLogLevel
    {
        /// <summary>
        /// Disable abnormal logging completely.
        /// </summary>
        None = 0,
        /// <summary>
        /// Log only errors and exceptions.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Log errors, exceptions, and abnormal cases (slow queries, large updates, large result sets).
        /// </summary>
        Warning = 2
    }

    /// <summary>
    /// 追蹤事件所屬的層級，用於區分系統中不同執行位置，
    /// 例如 UI、API 呼叫、API 服務、業務層或資料存取層。
    /// </summary>
    [Flags]
    public enum TraceLayer
    {
        /// <summary>
        /// 無層級（預設值）。
        /// </summary>
        None = 0,
        /// <summary>
        /// 使用者介面層，例如 WinForms、Blazor 或 MAUI 的操作。
        /// </summary>
        UI = 1 << 0,
        /// <summary>
        /// API 呼叫層，例如前端或外部系統呼叫 API。
        /// </summary>
        ApiClient = 1 << 1,
        /// <summary>
        /// API 服務層，例如後端 API Controller 或 Middleware。
        /// </summary>
        ApiServer = 1 << 2,
        /// <summary>
        /// 業務層，例如 Service 或 Domain Service 的執行。
        /// </summary>
        Business = 1 << 3,
        /// <summary>
        /// 資料存取層，例如 EF Core、Dapper 或 ADO.NET 的 SQL 執行。
        /// </summary>
        Data = 1 << 4,
        /// <summary>
        /// 所有層級（包含 UI、API 呼叫、API 服務、業務層與資料存取）。
        /// </summary>
        All = UI | ApiClient | ApiServer | Business | Data
    }

    /// <summary>
    /// 追蹤事件的種類，用於區分開始、結束與單點事件。
    /// </summary>
    public enum TraceEventKind
    {
        /// <summary>
        /// 代表一個追蹤區段的開始事件，
        /// 通常由 <see cref="ITraceListener.TraceStart"/> 產生。
        /// </summary>
        Start = 0,
        /// <summary>
        /// 代表一個追蹤區段的結束事件，
        /// 通常由 <see cref="ITraceListener.TraceEnd"/> 產生，
        /// 並包含該區段的耗時與執行狀態。
        /// </summary>
        End = 1,
        /// <summary>
        /// 代表一個單點事件，不需要成對呼叫，
        /// 通常由 <see cref="ITraceListener.TraceWrite"/> 產生。
        /// </summary>
        Point = 2
    }


    /// <summary>
    /// 追蹤事件的執行狀態，用於標示該次追蹤的結果。
    /// </summary>
    public enum TraceStatus
    {
        /// <summary>
        /// 成功完成，無錯誤或中斷。
        /// </summary>
        Ok,
        /// <summary>
        /// 發生錯誤，例如例外或執行失敗。
        /// </summary>
        Error,
        /// <summary>
        /// 執行被取消，例如使用者中斷或系統中止。
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 序列化狀態。
    /// </summary>
    public enum SerializeState
    {
        /// <summary>
        /// 無。
        /// </summary>
        None,
        /// <summary>
        /// 序列化。
        /// </summary>
        Serialize,
    }

    /// <summary>
    /// 序列化格式。
    /// </summary>
    public enum SerializeFormat
    {
        /// <summary>
        /// Xml 格式。
        /// </summary>
        Xml,
        /// <summary>
        /// Json 格式。
        /// </summary>
        Json,
        /// <summary>
        /// 二進位格式。
        /// </summary>
        Binary,
    }

    /// <summary>
    /// 欄位資料型別。
    /// </summary>
    public enum FieldDbType
    {
        /// <summary>
        /// 字串。
        /// </summary>
        String,
        /// <summary>
        /// 備註。
        /// </summary>
        Text,
        /// <summary>
        /// 布林。
        /// </summary>
        Boolean,
        /// <summary>
        /// 自動編號。
        /// </summary>
        Identity,
        /// <summary>
        /// 整數。
        /// </summary>
        Integer,
        /// <summary>
        /// 浮點數。
        /// </summary>
        Double,
        /// <summary>
        /// 貨幣。
        /// </summary>
        Currency,
        /// <summary>
        /// 日期。
        /// </summary>
        Date,
        /// <summary>
        /// 日期時間。
        /// </summary>
        DateTime,
        /// <summary>
        /// Guid 值。
        /// </summary>
        Guid,
        /// <summary>
        /// 二進位資料。
        /// </summary>
        Binary
    }

    /// <summary>
    /// 含預設值的布林列舉。
    /// </summary>
    public enum DefaultBoolean
    {
        /// <summary>
        /// 預設。
        /// </summary>
        Default,
        /// <summary>
        /// True。
        /// </summary>
        True,
        /// <summary>
        /// False。
        /// </summary>
        False
    }

    /// <summary>
    /// 含未設定的布林列舉。
    /// </summary>
    public enum NotSetBoolean
    {
        /// <summary>
        /// 未設定。
        /// </summary>
        NotSet,
        /// <summary>
        /// True。
        /// </summary>
        True,
        /// <summary>
        /// False。
        /// </summary>
        False
    }

    /// <summary>
    /// 時間間隔。
    /// </summary>
    public enum DateInterval
    {
        /// <summary>
        /// 年。
        /// </summary>
        Year = 0,
        /// <summary>
        /// 季 (1 到 4)
        /// </summary>
        Quarter = 1,
        /// <summary>
        /// 月份 (1 到 12)
        /// </summary>
        Month = 2,
        /// <summary>
        /// 年中的日 (1 到 366)
        /// </summary>
        DayOfYear = 3,
        /// <summary>
        /// 月中的日 (1 到 31)
        /// </summary>
        Day = 4,
        /// <summary>
        /// 年中的週 (1 到 53)
        /// </summary>
        WeekOfYear = 5,
        /// <summary>
        /// 星期資訊 (1 到 7)
        /// </summary>
        Weekday = 6,
        /// <summary>
        /// 小時 (1 到 24)
        /// </summary>
        Hour = 7,
        /// <summary>
        /// 分鐘 (1 到 60)
        /// </summary>
        Minute = 8,
        /// <summary>
        /// 秒鐘 (1 到 60)
        /// </summary>
        Second = 9
    }

    /// <summary>
    /// 背景服務狀態。
    /// </summary>
    public enum BackgroundServiceStatus
    {
        /// <summary>
        /// 停止。
        /// </summary>
        Stopped,
        /// <summary>
        /// 正在啟動。
        /// </summary>
        StartPending,
        /// <summary>
        /// 正在停止。
        /// </summary>
        StopPending,
        /// <summary>
        /// 執行中。
        /// </summary>
        Running
    }

    /// <summary>
    /// 背景服務執行動作。
    /// </summary>
    public enum BackgroundServiceAction
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        Initialize,
        /// <summary>
        /// 啟動。
        /// </summary>
        Start,
        /// <summary>
        /// 執行。
        /// </summary>
        Run,
        /// <summary>
        /// 停止。
        /// </summary>
        Stop
    }

    #endregion

}
