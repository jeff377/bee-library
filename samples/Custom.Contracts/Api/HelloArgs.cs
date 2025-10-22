using Bee.Contracts;
using MessagePack;
using System;

namespace Custom.Define
{
    [MessagePackObject]
    [Serializable]
    public class HelloArgs : BusinessArgs
    {
        /// <summary>
        /// 用戶名稱。
        /// </summary>
        [Key(100)]
        public string UserName { get; set; } = string.Empty;
    }
}
