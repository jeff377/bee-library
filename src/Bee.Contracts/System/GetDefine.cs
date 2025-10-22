using System;
using Bee.Define;
using MessagePack;

namespace Bee.Contracts
{
    /// <summary>
    /// 取得定義資料的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetDefineArgs : BusinessArgs
    {
        /// <summary>
        /// 定義資料類型。
        /// </summary>
        [Key(100)]
        public DefineType DefineType { get; set; }

        /// <summary>
        /// 取得定義資料的鍵值。
        /// </summary>
        [Key(101)]
        public string[] Keys { get; set; } = null;
    }

    /// <summary>
    ///  取得定義資料的傳出結果
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetDefineResult : BusinessResult
    {
        /// <summary>
        /// 定義資料。
        /// </summary>
        [Key(100)]
        public string Xml { get; set; } = string.Empty;
    }
}
