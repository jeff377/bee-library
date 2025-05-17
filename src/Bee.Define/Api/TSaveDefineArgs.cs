using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 儲存定義資料的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class TSaveDefineArgs : TBusinessArgs
    {
        /// <summary>
        /// 定義資料類型。
        /// </summary>
        [Key(100)]
        public EDefineType DefineType { get; set; }

        /// <summary>
        /// 定義資料。
        /// </summary>
        [Key(101)]
        public object DefineObject { get; set; } = null;

        /// <summary>
        /// 儲存定義資料的鍵值。
        /// </summary>
        [Key(102)]
        public string[] Keys { get; set; } = null;
    }
}
