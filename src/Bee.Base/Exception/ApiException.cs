using System;
using System.ComponentModel;

namespace Bee.Base
{
    /// <summary>
    /// 執行 API 方法的例外錯誤。
    /// </summary>
    [Serializable]
    public class ApiException : IObjectSerializeBase
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ApiException()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="exception">執行期間的例外錯誤。</param>
        public ApiException(Exception exception)
        {
            Message = exception.Message;
            StackTrace = exception.StackTrace;
        }

        #endregion

        /// <summary>
        /// 例外錯誤訊息。
        /// </summary>
        [DefaultValue("")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 呼叫堆疊。
        /// </summary>
        [DefaultValue("")]
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// 是否為已處理的例外。
        /// </summary>
        [Description("是否為已處理的例外。")]
        [DefaultValue(false)]
        public bool IsHandle { get; set; } = false;

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return this.Message;
        }
    }
}
