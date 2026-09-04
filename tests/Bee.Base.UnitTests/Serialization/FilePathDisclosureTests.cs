using System.ComponentModel;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Serialization
{
    /// <summary>
    /// 定義檔反序列化失敗時，例外訊息不得洩漏伺服器的目錄配置。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這不是理論上的風險：<see cref="InvalidOperationException"/> 在 JSON-RPC 的錯誤契約裡映射為
    /// <c>UserMessage</c>，而該分支把 <c>ex.Message</c> **原樣**回傳給呼叫端。於是一個已認證的
    /// 遠端呼叫者只要打到一個損毀的定義檔，就會拿到伺服器的絕對路徑。
    /// 違反 <c>scanning.md</c>「例外訊息禁止包含內部路徑」。
    /// </para>
    /// <para>
    /// 完整路徑改放 <see cref="Exception.Data"/>，只有伺服端的記錄看得到。
    /// </para>
    /// </remarks>
    public class FilePathDisclosureTests
    {
        private static string WriteCorruptFile(string extension)
        {
            // 目錄名刻意帶可辨識字樣：訊息若洩漏路徑，斷言就抓得到是哪一段。
            string dir = Path.Combine(Path.GetTempPath(), "bee-secret-layout-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "broken" + extension);
            File.WriteAllText(file, "this is not valid content");
            return file;
        }

        [Theory]
        [InlineData(".xml")]
        [InlineData(".json")]
        [DisplayName("反序列化失敗的訊息只能有檔名，不得含路徑；完整路徑放在 Exception.Data")]
        public void DeserializeFromFile_Corrupt_MessageNamesFileNotPath(string extension)
        {
            string file = WriteCorruptFile(extension);
            string dir = Path.GetDirectoryName(file)!;
            try
            {
                var ex = Assert.Throws<InvalidOperationException>(() => extension == ".xml"
                    ? XmlCodec.DeserializeFromFile<SampleValue>(file)
                    : JsonCodec.DeserializeFromFile<SampleValue>(file));

                // 對照組：訊息確實提到了這個檔，斷言才不是因為訊息空白而恆真。
                Assert.Contains(Path.GetFileName(file), ex.Message, StringComparison.Ordinal);

                Assert.DoesNotContain(dir, ex.Message, StringComparison.Ordinal);
                Assert.DoesNotContain("bee-secret-layout-", ex.Message, StringComparison.Ordinal);

                // 完整路徑仍拿得到 —— 給伺服端記錄用，不給呼叫端。
                Assert.Equal(file, ex.Data[SerializationErrorData.FilePath]);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        /// <summary>反序列化目標；內容刻意壞掉，所以型別本身不重要。</summary>
        public class SampleValue
        {
            /// <summary>任意屬性。</summary>
            public string Name { get; set; } = string.Empty;
        }
    }
}
