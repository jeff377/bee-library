using System.Text;

namespace Bee.Base.UnitTests
{
    public class GZipFuncTests
    {
        /// <summary>
        /// 測試壓縮功能，檢查資料是否能成功壓縮。
        /// </summary>
        [Fact]
        public void TestCompress()
        {
            // 輸入資料：較大的字串資料
            string originalText = new string('A', 1024); // 1024 個 'A' 字符
            byte[] originalBytes = Encoding.UTF8.GetBytes(originalText);

            // 執行壓縮
            byte[] compressedData = GZipFunc.Compress(originalBytes);

            // 驗證壓縮後的資料不為空
            Assert.NotNull(compressedData);
            Assert.NotEmpty(compressedData);

            // 驗證壓縮後的資料可以成功解壓縮
            byte[] decompressedData = GZipFunc.Uncompress(compressedData);
            string decompressedText = Encoding.UTF8.GetString(decompressedData);

            // 驗證解壓縮後的資料與原始資料一致
            Assert.Equal(originalText, decompressedText);
        }

        /// <summary>
        /// 測試解壓縮功能，確保壓縮和解壓縮能正常工作。
        /// </summary>
        [Fact]
        public void TestUncompress()
        {
            // 輸入資料：原始資料字串
            string originalText = "這是要進行解壓縮的測試資料！";
            byte[] originalBytes = Encoding.UTF8.GetBytes(originalText);

            // 執行壓縮
            byte[] compressedData = GZipFunc.Compress(originalBytes);

            // 執行解壓縮
            byte[] uncompressedData = GZipFunc.Uncompress(compressedData);

            // 驗證解壓縮後的資料不為空
            Assert.NotNull(uncompressedData);
            Assert.NotEmpty(uncompressedData);

            // 驗證解壓縮後的資料與原始資料相同
            string uncompressedText = Encoding.UTF8.GetString(uncompressedData);
            Assert.Equal(originalText, uncompressedText);
        }
    }
}
