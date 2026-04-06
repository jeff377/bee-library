using System.IO;
using System.IO.Compression;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Utility library for GZip compression and decompression.
    /// </summary>
    public static class GZipFunc
    {
        /// <summary>
        /// Compresses the specified byte array using GZip.
        /// </summary>
        /// <param name="bytes">The raw byte data to compress.</param>
        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    gZipStream.Write(bytes, 0, bytes.Length);
                }
                return stream.ToArray(); // Ensure all data is flushed and converted to a byte array
            }
        }

        /// <summary>
        /// Decompresses the specified GZip-compressed byte array.
        /// </summary>
        /// <param name="bytes">The compressed byte data to decompress.</param>
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
