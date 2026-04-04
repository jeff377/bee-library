namespace Bee.Base
{
    /// <summary>
    /// 測試連線結果類別。
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public ConnectionTestResult()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="isSuccess">是否成功。</param>
        /// <param name="message">錯誤或狀態訊息。</param>
        public ConnectionTestResult(bool isSuccess, string message)
        {
            this.IsSuccess = isSuccess;
            this.Message = message;
        }

        /// <summary>
        /// 是否成功。
        /// </summary>
        public bool IsSuccess { get; set; } = false;

        /// <summary>
        /// 錯誤或狀態訊息。
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
