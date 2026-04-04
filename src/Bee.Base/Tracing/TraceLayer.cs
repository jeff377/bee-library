using System;

namespace Bee.Base.Tracing
{
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
}
