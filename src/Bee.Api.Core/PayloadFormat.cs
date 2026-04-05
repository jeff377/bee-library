
namespace Bee.Api.Core
{
    /// <summary>
    /// 傳輸資料的封裝格式。
    /// </summary>
    public enum PayloadFormat
    {
        /// <summary>
        /// 原始格式（未經編碼或加密）。
        /// </summary>
        Plain,

        /// <summary>
        /// 編碼格式（已序列化並壓縮）。
        /// </summary>
        Encoded,

        /// <summary>
        /// 加密格式（序列化 + 壓縮 + 加密）。
        /// </summary>
        Encrypted
    }
}
