using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 預設 API 服務傳輸資料加密物件。
    /// </summary>
    public class TApiServiceEncryption : IApiServiceEncryption
    {
        /// <summary>
        /// 資料進行加密。
        /// </summary>
        /// <param name="bytes">未加密資料。</param>
        public byte[] Encrypt(byte[] bytes)
        {
            byte[] oBytes;

            oBytes = GZipFunc.Compress(bytes);  // 壓縮
            return CryptoFunc.AesEncrypt(oBytes);  // 加密
        }

        /// <summary>
        /// 資料進行解密。
        /// </summary>
        /// <param name="bytes">已加密資料。</param>
        public byte[] Decrypt(byte[] bytes)
        {
            byte[] oBytes;

            oBytes = CryptoFunc.AesDecrypt(bytes);  // 解密
            return GZipFunc.Uncompress(oBytes);  // 解壓縮
        }
    }
}
