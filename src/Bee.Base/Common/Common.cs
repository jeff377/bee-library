namespace Bee.Base
{
    #region 常數

    /// <summary>
    /// 屬性視窗的顯示分類常數。
    /// </summary>
    public class Category
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

    #endregion

    #region 列舉型別

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
