using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 儲存定義資料的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SaveDefineArgs : BusinessArgs
    {
        /// <summary>
        /// 定義資料類型。
        /// </summary>
        [Key(100)]
        public DefineType DefineType { get; set; }

        /// <summary>
        /// 定義資料。
        /// </summary>
        [Key(101)]
        public string Xml { get; set; } = string.Empty;

        /// <summary>
        /// 儲存定義資料的鍵值。
        /// </summary>
        [Key(102)]
        public string[] Keys { get; set; } = null;
    }

    /// <summary>
    ///  儲存定義資料的傳出結果
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SaveDefineResult : BusinessResult
    {
    }
}
