using System;

namespace Bee.Define
{
    /// <summary>
    /// 執行自訂方法的傳入引數。
    /// </summary>
    [Serializable]
    public class TExecFuncArgs : TBusinessArgs
    {
        private string _FuncID = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TExecFuncArgs()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="funcID">自訂方法識別編號。</param>
        public TExecFuncArgs(string funcID)
        {
            _FuncID = funcID;
        }

        #endregion

        /// <summary>
        /// 自訂方法識別編號。
        /// </summary>
        public string FuncID
        {
            get { return _FuncID; }
            set { _FuncID = value; }
        }
    }
}
