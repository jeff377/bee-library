using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 執行自訂方法的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ExecFuncArgs : BusinessArgs
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ExecFuncArgs()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="funcID">自訂方法識別編號。</param>
        public ExecFuncArgs(string funcID)
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
