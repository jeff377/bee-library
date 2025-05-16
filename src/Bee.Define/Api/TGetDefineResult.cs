using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    ///  取得定義資料的傳出結果
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class TGetDefineResult : TBusinessResult
    {
        /// <summary>
        /// 定義資料。
        /// </summary>
        [Key(100)]
        public string Xml { get; set; } = string.Empty;
    }
}
