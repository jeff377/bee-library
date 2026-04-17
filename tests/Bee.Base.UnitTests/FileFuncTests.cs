using System.ComponentModel;
using System.Text;

namespace Bee.Base.UnitTests
{
    public class FileFuncTests : IDisposable
    {
        private readonly string _tempDir;

        public FileFuncTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "bee-base-filefunc-" + Guid.NewGuid().ToString("N"));
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
        }

        private string TempPath(string relative) => Path.Combine(_tempDir, relative);

        [Fact]
        [DisplayName("FileExists 應正確回報檔案存在與否")]
        public void FileExists_ReflectsFilesystemState()
        {
            string path = TempPath("exists.txt");
            Assert.False(FileFunc.FileExists(path));

            File.WriteAllText(path, "hello");
            Assert.True(FileFunc.FileExists(path));
        }

        [Fact]
        [DisplayName("FileDelele 應刪除存在的檔案，對不存在的檔案不拋例外")]
        public void FileDelele_RemovesExistingFileAndIgnoresMissing()
        {
            string path = TempPath("delete-me.txt");
            File.WriteAllText(path, "x");

            FileFunc.FileDelele(path);
            Assert.False(File.Exists(path));

            // Calling again on a missing file must not throw.
            FileFunc.FileDelele(path);
        }

        [Fact]
        [DisplayName("BytesToFile / FileToBytes 應 round-trip 二進位資料")]
        public void BytesToFileAndFileToBytes_Roundtrip()
        {
            string path = TempPath("bytes.bin");
            byte[] data = [0x01, 0x02, 0x03, 0xFF];

            FileFunc.BytesToFile(data, path);
            byte[] read = FileFunc.FileToBytes(path);

            Assert.Equal(data, read);
        }

        [Fact]
        [DisplayName("BytesToFile 若目錄不存在應自動建立")]
        public void BytesToFile_CreatesMissingDirectory()
        {
            string path = TempPath("nested/deeper/file.bin");
            FileFunc.BytesToFile([0xAA], path);

            Assert.True(File.Exists(path));
        }

        [Fact]
        [DisplayName("FileWriteText 以 UTF-8 無 BOM 寫入並可被 FileReadText 讀回")]
        public void FileWriteTextDefaultEncoding_IsUtf8NoBom()
        {
            string path = TempPath("text.txt");
            FileFunc.FileWriteText(path, "哈囉 World");

            byte[] raw = File.ReadAllBytes(path);
            // BOM for UTF-8 is EF BB BF; default should omit it.
            Assert.False(raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF);

            Assert.Equal("哈囉 World", FileFunc.FileReadText(path));
        }

        [Fact]
        [DisplayName("FileWriteText 可指定編碼並正確寫入")]
        public void FileWriteText_WithExplicitEncoding_WritesBytes()
        {
            string path = TempPath("encoded.txt");
            var utf16 = new UnicodeEncoding();
            FileFunc.FileWriteText(path, "Hi", utf16);

            byte[] raw = File.ReadAllBytes(path);
            Assert.Equal(utf16.GetPreamble().Length + 4, raw.Length);
        }

        [Fact]
        [DisplayName("FileReadText 於檔案不存在時應回傳空字串")]
        public void FileReadText_MissingFile_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, FileFunc.FileReadText(TempPath("no-file.txt")));
        }

        [Fact]
        [DisplayName("StreamToFile 與 FileToStream 應 round-trip 內容")]
        public void StreamToFileAndFileToStream_Roundtrip()
        {
            string path = TempPath("stream.bin");
            byte[] data = Encoding.UTF8.GetBytes("stream payload");

            using (var mem = new MemoryStream(data))
            {
                mem.Position = 5; // StreamToFile should rewind the stream before copying.
                FileFunc.StreamToFile(mem, path);
            }

            using var stream = FileFunc.FileToStream(path);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            Assert.Equal("stream payload", reader.ReadToEnd());
        }

        [Fact]
        [DisplayName("DirectoryExists / DirectoryCreate / DirectoryCheck 應互相協作")]
        public void DirectoryHelpers_CreateAndCheck()
        {
            string dir = TempPath("dir-a");
            Assert.False(FileFunc.DirectoryExists(dir));

            FileFunc.DirectoryCreate(dir);
            Assert.True(FileFunc.DirectoryExists(dir));

            string file = TempPath("new-dir/file.txt");
            FileFunc.DirectoryCheck(file, isFilePath: true);
            Assert.True(Directory.Exists(TempPath("new-dir")));

            string dirPath = TempPath("plain-dir");
            FileFunc.DirectoryCheck(dirPath);
            Assert.True(Directory.Exists(dirPath));
        }

        [Fact]
        [DisplayName("Path 系列方法應提供正確的檔名、副檔名與目錄")]
        public void PathHelpers_ReturnExpectedParts()
        {
            string combined = FileFunc.PathCombine("a", "b", "c.txt");
            Assert.EndsWith("c.txt", combined);

            Assert.Equal(".txt", FileFunc.GetExtension("x/y/z.txt"));
            Assert.Equal("z.txt", FileFunc.GetFileName("x/y/z.txt"));
            Assert.Equal("z", FileFunc.GetFileName("x/y/z.txt", isExtension: false));
            Assert.EndsWith("y", FileFunc.GetDirectory("x/y/z.txt"));
        }

        [Fact]
        [DisplayName("GetParentDirectory 應回傳上層目錄的完整路徑")]
        public void GetParentDirectory_ReturnsFullPath()
        {
            string child = TempPath("child-dir");
            Directory.CreateDirectory(child);

            string parent = FileFunc.GetParentDirectory(child);
            Assert.Equal(Path.GetFullPath(_tempDir), Path.GetFullPath(parent));
        }

        [Theory]
        [InlineData(@"C:\temp\file.txt", true)]
        [InlineData(@"\\server\share\file.txt", true)]
        [InlineData("relative/path", false)]
        [InlineData("http://example.com", false)]
        [DisplayName("IsLocalPath 應辨識 Windows 與 UNC 路徑")]
        public void IsLocalPath_RecognizesLocalFormats(string input, bool expected)
        {
            Assert.Equal(expected, FileFunc.IsLocalPath(input));
        }

        [Theory]
        [InlineData(@"C:\temp\file.txt", true)]
        [InlineData("/etc/hosts", true)]
        [InlineData("relative/path", false)]
        [DisplayName("IsPathRooted 應辨識絕對路徑")]
        public void IsPathRooted_RecognizesAbsolutePaths(string input, bool expected)
        {
            Assert.Equal(expected, FileFunc.IsPathRooted(input));
        }

        [Fact]
        [DisplayName("GetAppPath 空子路徑回傳基底路徑，有子路徑則組合")]
        public void GetAppPath_AppendsSubPath()
        {
            string basePath = FileFunc.GetAppPath();
            Assert.False(string.IsNullOrEmpty(basePath));

            string sub = FileFunc.GetAppPath("config");
            Assert.EndsWith("config", sub.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        [Fact]
        [DisplayName("GetAssemblyPath 應回傳非空路徑")]
        public void GetAssemblyPath_ReturnsNonEmptyPath()
        {
            string path = FileFunc.GetAssemblyPath();
            Assert.False(string.IsNullOrEmpty(path));
        }
    }
}
