using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 執行自訂方法的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class TExecFuncArgs : TBusinessArgs
    {
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
            FuncID = funcID;
        }

        #endregion

        /// <summary>
        /// 自訂方法識別編號。
        /// </summary>
        [Key(100)]
        public string FuncID { get; set; } = string.Empty;

    }
}
