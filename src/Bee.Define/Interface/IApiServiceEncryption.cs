namespace Bee.Define
{
    /// <summary>
    /// API 服務傳輸資料加密介面。
    /// </summary>
    public interface IApiServiceEncryption
    {
        /// <summary>
        /// 資料進行加密。
        /// </summary>
        /// <param name="bytes">未加密資料。</param>
        byte[] Encrypt(byte[] bytes);

        /// <summary>
        /// 資料進行解密。
        /// </summary>
        /// <param name="bytes">已加密資料。</param>
        byte[] Decrypt(byte[] bytes);
    }
}
