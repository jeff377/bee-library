using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 系統日誌事件的記錄物件，參照 EventLogEntry 設計。
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 記錄類型，例如：資訊、警告或錯誤。
        /// </summary>
        public LogEntryType EntryType { get; set; }

        /// <summary>
        /// 來源模組或元件名稱，例如 "EmployeeBusinessObject"。
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 分類代碼，可用於區分功能模組或子系統。
        /// </summary>
        public short Category { get; set; }

        /// <summary>
        /// 主機名稱或執行程式所在機器名稱。
        /// </summary>
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// 主要訊息內容。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 發生時間（預設為現在）。
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 可選的例外物件。
        /// </summary>
        public Exception Exception { get; set; }
    }
}
