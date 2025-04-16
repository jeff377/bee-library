using System;

namespace Bee.Define
{
    /// <summary>
    /// 取得定義資料的傳入引數。
    /// </summary>
    [Serializable]
    public class TGetDefineArgs : TBusinessArgs
    {
        /// <summary>
        /// 定義資料類型。
        /// </summary>
        public EDefineType DefineType { get; set; }

        /// <summary>
        /// 取得定義資料的鍵值。
        /// </summary>
        public string[] Keys { get; set; } = null;  
    }
}
