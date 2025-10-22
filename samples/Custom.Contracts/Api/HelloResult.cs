using Bee.Contracts;
using MessagePack;
using System;

namespace Custom.Define
{
    [MessagePackObject]
    [Serializable]
    public class HelloResult : BusinessResult
    {
        /// <summary>
        /// 回傳訊息。
        /// </summary>
        [Key(100)]
        public string Message { get; set; } = string.Empty;
    }
}
