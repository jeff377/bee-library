namespace Bee.Base.Tracing
{
    /// <summary>
    /// 定義常用的追蹤分類常數，供 TraceEvent.Category 使用。
    /// </summary>
    public static class TraceCategories
    {
        /// <summary>
        /// 一般用途。
        /// </summary>
        public const string General = "General";
        /// <summary>
        /// JSON-RPC 請求或回應模型。
        /// </summary>
        public const string JsonRpc = "JsonRpc";
    }
}
