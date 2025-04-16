using System;

namespace Bee.Base
{
    /// <summary>
    /// 執行期間的例外錯誤。
    /// </summary>
    public class TException : Exception
    {
        private bool _IsHandle = true;

        #region 建構函式

        /// <summary>
        /// 建構函式
        /// </summary>
        public TException() : base() { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="message">錯誤訊息。</param>
        /// <param name="args">參數陣列。</param>
        public TException(string message, params object[] args)
          : base(StrFunc.Format(message, args))
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="isHandle">是否為已處理的例外。</param>
        /// <param name="message">錯誤訊息。</param>
        /// <param name="args">參數陣列。</param>
        public TException(bool isHandle, string message, params object[] args)
          : base(StrFunc.Format(message, args))
        {
            _IsHandle = isHandle;
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="message">錯誤訊息。</param>
        /// <param name="innerException">內部例外參考。</param>
        public TException(string message, Exception innerException)
          : base(message, innerException)
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="isHandle">是否為已處理的例外。</param>
        /// <param name="message">錯誤訊息。</param>
        /// <param name="innerException">內部例外參考。</param>
        public TException(bool isHandle, string message, Exception innerException)
          : base(message, innerException)
        {
            _IsHandle = isHandle;
        }

        #endregion

        /// <summary>
        /// 是否為已處理的例外。
        /// </summary>
        public bool IsHandle
        {
            get { return _IsHandle; }
            set { _IsHandle = value; }
        }
    }
}
