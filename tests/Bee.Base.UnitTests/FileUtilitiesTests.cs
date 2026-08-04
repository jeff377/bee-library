using System.ComponentModel;
using System.Text;

namespace Bee.Base.UnitTests
{
    public class FileUtilitiesTests : IDisposable
    {
        private readonly string _tempDir;

        public FileUtilitiesTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "bee-base-fileutils-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch (IOException)
            {
                // Temp files may still be held by test runner; ignore on teardown.
            }
            catch (UnauthorizedAccessException)
            {
                // Temp files may still be held by test runner; ignore on teardown.
            }
            GC.SuppressFinalize(this);
        }

        private string TempPath(string relative) => Path.Combine(_tempDir, relative);

        [Fact]
        [DisplayName("FileWriteTextAtomic 應以 UTF-8 no BOM 寫入並可讀回")]
        public void FileWriteTextAtomic_DefaultEncoding_NoBom()
        {
            string path = TempPath("atomic.txt");
            FileUtilities.FileWriteTextAtomic(path, "哈囉 World");

            byte[] raw = File.ReadAllBytes(path);
            Assert.False(raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF,
                "FileWriteTextAtomic 不應寫入 UTF-8 BOM");
            Assert.Equal("哈囉 World", FileUtilities.FileReadText(path));
        }

        [Fact]
        [DisplayName("FileWriteTextAtomic 應取代既有檔案（三參數 File.Move 的 overwrite 語意）")]
        public void FileWriteTextAtomic_ExistingFile_IsReplaced()
        {
            string path = TempPath("atomic-replace.txt");
            FileUtilities.FileWriteTextAtomic(path, "OLD");

            FileUtilities.FileWriteTextAtomic(path, "NEW");

            Assert.Equal("NEW", FileUtilities.FileReadText(path));
        }

        [Fact]
        [DisplayName("FileWriteTextAtomic 不應留下暫存檔")]
        public void FileWriteTextAtomic_LeavesNoTempFile()
        {
            string dir = TempPath("atomic-dir");
            string path = Path.Combine(dir, "target.xml");

            FileUtilities.FileWriteTextAtomic(path, "<root/>");

            Assert.Equal("target.xml", Assert.Single(Directory.GetFiles(dir).Select(Path.GetFileName)));
        }

        [Fact]
        [DisplayName("FileWriteTextAtomic 應自動建立目標目錄")]
        public void FileWriteTextAtomic_CreatesDirectory()
        {
            string path = TempPath(Path.Combine("nested", "deeper", "file.txt"));

            FileUtilities.FileWriteTextAtomic(path, "x");

            Assert.True(File.Exists(path));
        }

        [Fact]
        [DisplayName("FileWriteText 預設應以 UTF-8 no BOM 寫入並可讀回")]
        public void FileWriteText_DefaultEncoding_NoBom()
        {
            string path = TempPath("write.txt");
            FileUtilities.FileWriteText(path, "哈囉 World");

            byte[] raw = File.ReadAllBytes(path);
            // 0xEF 0xBB 0xBF is the UTF-8 BOM
            Assert.False(raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF,
                "FileWriteText 預設不應寫入 UTF-8 BOM");

            Assert.Equal("哈囉 World", FileUtilities.FileReadText(path));
        }

        [Fact]
        [DisplayName("FileWriteText 應依指定編碼寫入(UTF-16)")]
        public void FileWriteText_ExplicitEncoding_WritesAccordingly()
        {
            string path = TempPath("utf16.txt");
            var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            FileUtilities.FileWriteText(path, "Hi", utf16);

            byte[] raw = File.ReadAllBytes(path);
            // UTF-16 LE BOM = 0xFF 0xFE
            Assert.True(raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE);
        }

        [Fact]
        [DisplayName("FileReadText 於檔案不存在時應回傳空字串")]
        public void FileReadText_MissingFile_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, FileUtilities.FileReadText(TempPath("no-file.txt")));
        }

        [Fact]
        [DisplayName("FileWriteText 應自動建立目標子目錄")]
        public void FileWriteText_AutoCreatesParentDirectory()
        {
            string nestedDir = TempPath("auto/nested");
            Assert.False(Directory.Exists(nestedDir));

            string path = Path.Combine(nestedDir, "deep.txt");
            FileUtilities.FileWriteText(path, "ok");

            Assert.True(File.Exists(path));
            Assert.Equal("ok", FileUtilities.FileReadText(path));
        }

        [Theory]
        [InlineData(@"C:\temp\file.txt", true)]
        [InlineData(@"D:\folder", true)]
        [InlineData(@"\\server\share\file", true)]
        [InlineData("http://example.com", false)]
        [InlineData("relative/path", false)]
        [InlineData("", false)]
        [DisplayName("IsLocalPath 應辨識 Windows drive 與 UNC 路徑")]
        public void IsLocalPath_RecognizesLocalPaths(string input, bool expected)
        {
            Assert.Equal(expected, FileUtilities.IsLocalPath(input));
        }

        [Fact]
        [DisplayName("GetAssemblyPath 應回傳非空目錄字串")]
        public void GetAssemblyPath_ReturnsNonEmpty()
        {
            string path = FileUtilities.GetAssemblyPath();
            Assert.False(string.IsNullOrEmpty(path));
        }
    }
}
