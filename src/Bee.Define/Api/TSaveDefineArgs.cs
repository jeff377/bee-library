using System;

namespace Bee.Define
{
    /// <summary>
    /// 儲存定義資料的傳入引數。
    /// </summary>
    [Serializable]
    public class TSaveDefineArgs : TBusinessArgs
    {
        /// <summary>
        /// 定義資料類型。
        /// </summary>
        public EDefineType DefineType { get; set; }

        /// <summary>
        /// 定義資料。
        /// </summary>
        public object DefineObject { get; set; } = null;

        /// <summary>
        /// 儲存定義資料的鍵值。
        /// </summary>
        public string[] Keys { get; set; } = null;
    }
}
