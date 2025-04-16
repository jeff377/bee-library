using System;
using System.ComponentModel;

namespace Bee.Base
{
    /// <summary>
    /// 執行 API 方法的例外錯誤。
    /// </summary>
    [Serializable]
    public class TApiException : IObjectSerializeBase
    {
        private string _Message = string.Empty;
        private string _StackTrace = string.Empty;
        private bool _IsHandle = false;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TApiException()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="exception">執行期間的例外錯誤。</param>
        public TApiException(Exception exception)
        {
            _Message = exception.Message;
            _StackTrace = exception.StackTrace;
            if (exception is TException)
                _IsHandle = (exception as TException).IsHandle;
            else
                _IsHandle = false;
        }

        #endregion

        /// <summary>
        /// 例外錯誤訊息。
        /// </summary>
        [DefaultValue("")]
        public string Message
        {
            get { return _Message; }
            set { _Message = value; }
        }

        /// <summary>
        /// 呼叫堆疊。
        /// </summary>
        [DefaultValue("")]
        public string StackTrace
        {
            get { return _StackTrace; }
            set { _StackTrace = value; }
        }

        /// <summary>
        /// 是否為已處理的例外。
        /// </summary>
        [Description("是否為已處理的例外。")]
        [DefaultValue(false)]
        public bool IsHandle
        {
            get { return _IsHandle; }
            set { _IsHandle = value; }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return this.Message;
        }
    }
}
