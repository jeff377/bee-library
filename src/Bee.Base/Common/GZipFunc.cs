using System.IO;
using System.IO.Compression;

namespace Bee.Base
{
    /// <summary>
    /// GZip 壓縮函式庫。
    /// </summary>
    public static class GZipFunc
    {
        /// <summary>
        /// 執行壓縮。
        /// </summary>
        /// <param name="bytes">原始位元組資料。</param>
        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    gZipStream.Write(bytes, 0, bytes.Length);
                }
                return stream.ToArray(); // 確保所有資料都被寫入並轉換為位元組陣列
            }
        }

        /// <summary>
        /// 執行解壓縮。
        /// </summary>
        /// <param name="bytes">壓縮後的位元組資料。</param>
        public static byte[] Decompress(byte[] bytes)
        {
            byte[] buffer = new byte[4096];
            int count;

            using (MemoryStream inputStream = new MemoryStream(bytes))
            {
                using (GZipStream gZipStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        while ((count = gZipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outputStream.Write(buffer, 0, count);
                        }
                        return outputStream.ToArray();
                    }
                }
            }
        }
    }


}
