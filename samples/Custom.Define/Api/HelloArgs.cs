using System;
using Bee.Define;
using MessagePack;

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
