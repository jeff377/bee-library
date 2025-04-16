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
        /// <param name="bytes">原始二進位資料。</param>
        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    gZipStream.Write(bytes, 0, bytes.Length);
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 執行解壓縮。
        /// </summary>
        /// <param name="bytes">壓縮二進位資料。</param>
        public static byte[] Uncompress(byte[] bytes)
        {
            byte[] oBuffer = new byte[4096];
            int iRead;

            using (MemoryStream inputStream = new MemoryStream())
            {
                inputStream.Write(bytes, 0, bytes.Length);
                inputStream.Position = 0;
                using (GZipStream gZipStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        iRead = gZipStream.Read(oBuffer, 0, oBuffer.Length);
                        while (iRead > 0)
                        {
                            outputStream.Write(oBuffer, 0, iRead);
                            iRead = gZipStream.Read(oBuffer, 0, oBuffer.Length);
                        }
                        return outputStream.ToArray();
                    }
                }
            }
        }
    }
}
