using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bee.Base
{
    /// <summary>
    /// 檔案存取函式庫。
    /// </summary>
    public static class FileFunc
    {
        #region 檔案相關方法

        /// <summary>
        /// 判斷指定檔案是否存在。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// 檔案刪除。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public static void FileDelele(string filePath)
        {
            if (FileFunc.FileExists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// 檔案轉為二進位資料。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public static byte[] FileToBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// 二進位資料轉為檔案。
        /// </summary>
        /// <param name="bytes">二進位資料。</param>
        /// <param name="filePath">檔案路徑。</param>
        public static void BytesToFile(byte[] bytes, string filePath)
        {
            // 判斷目錄是否存在，不存在則建立
            DirectoryCheck(filePath, true);
            // 二進位資料寫入檔案
            File.WriteAllBytes(filePath, bytes);
        }

        /// <summary>
        /// 檔案轉為串流。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public static Stream FileToStream(string filePath)
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        /// <summary>
        /// 串流轉為檔案。
        /// </summary>
        /// <param name="stream">串流。</param>
        /// <param name="filePath">檔案路徑。</param>
        public static void StreamToFile(Stream stream, string filePath)
        {
            // 判斷目錄是否存在，不存在則建立
            DirectoryCheck(filePath, true);

            // 設置資料流的起始位置
            stream.Seek(0, SeekOrigin.Begin);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.CopyTo(fileStream);
            }
        }

        /// <summary>
        /// 寫入文字檔，然後關閉檔案。若檔案已存在則覆蓋，預設編碼為 UTF8。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <param name="contents">要寫入檔案的字串。</param>
        public static void FileWriteText(string filePath, string contents)
        {
            // UTF8Encoding(false) 為不含位元組順序標記（BOM）的 UTF-8 編碼
            FileWriteText(filePath, contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// 寫入文字檔，然後關閉檔案。若檔案已存在則覆蓋。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        /// <param name="contents">要寫入檔案的字串。</param>
        /// <param name="encoding">編碼方式。</param>
        public static void FileWriteText(string filePath, string contents, Encoding encoding)
        {
            // 判斷目錄是否存在，不存在則建立
            DirectoryCheck(filePath, true);

            File.WriteAllText(filePath, contents, encoding);
        }

        /// <summary>
        /// 讀取文字檔。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public static string FileReadText(string filePath)
        {
            if (!FileFunc.FileExists(filePath))
                return string.Empty;
            else
                return File.ReadAllText(filePath);
        }

        #endregion

        #region 目錄相關方法

        /// <summary>
        /// 判斷目錄是否存在。
        /// </summary>
        /// <param name="path">要檢查的目錄。</param>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// 建立目錄。
        /// </summary>
        /// <param name="path">指定路徑。</param>
        public static void DirectoryCreate(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// 判斷目錄是否存在，不存在則建立。
        /// </summary>
        /// <param name="path">要檢查的路徑。</param>
        /// <param name="isFilePath">傳入是否為檔案路徑，檔案路徑要先取得目錄，再判斷目錄是否存在。</param>
        public static void DirectoryCheck(string path, bool isFilePath = false)
        {
            string sPath;

            // 取得目錄路徑
            sPath = (isFilePath) ? GetDirectory(path) : path;
            // 判斷目錄是否存在，不存在則建立
            if (!DirectoryExists(sPath))
                DirectoryCreate(sPath);
        }

        #endregion

        #region 路徑相關方法

        /// <summary>
        /// 判斷是否為本地路徑。
        /// </summary>
        /// <param name="input">輸入路徑。</param>
        public static bool IsLocalPath(string input)
        {
            // 判斷是否為 Windows 路徑或 UNC 網芳路徑
            string pattern = @"^([a-zA-Z]:\\|\\\\)";
            return Regex.IsMatch(input, pattern);
        }

        /// <summary>
        /// 取得目錄。
        /// </summary>
        /// <param name="path">路徑字串。</param>
        public static string GetDirectory(string path)
        {
           return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// 取得上一層目錄。
        /// </summary>
        /// <param name="path">路徑字串。</param>
        public static string GetParentDirectory(string path)
        {
            return Directory.GetParent(path).FullName;
        }

        /// <summary>
        /// 取得路徑字串的檔名及副檔名。
        /// </summary>
        /// <param name="path">路徑字串。</param>
        /// <param name="isExtension">是否包含附檔名。</param>
        public static string GetFileName(string path, bool isExtension = true)
        {
            if (isExtension)
                return Path.GetFileName(path);
            else
                return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// 取得檔案路徑的副檔名。
        /// </summary>
        /// <param name="path">檔案路徑。</param>
        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        /// <summary>
        /// 將一個字串陣列合併為單一路徑。
        /// </summary>
        /// <param name="paths">路徑中各部分的陣列。</param>
        /// <remarks>合併的路徑。</remarks>
        public static string PathCombine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <summary>
        /// 取得應用程式路徑或子路徑。
        /// </summary>
        /// <param name="subPath">子路徑。</param>
        public static string GetAppPath(string subPath ="")
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (StrFunc.IsNotEmpty(subPath))
                path = PathCombine(path, subPath);
            return path;
        }

        /// <summary>
        /// 取得應用程式私用組件目錄。
        /// </summary>
        public static string GetAssemblyPath()
        {
            // 取得程式組件路徑
            if (StrFunc.IsEmpty(AppDomain.CurrentDomain.RelativeSearchPath))
                return AppDomain.CurrentDomain.BaseDirectory;
            else
                return AppDomain.CurrentDomain.RelativeSearchPath;
        }

        #endregion
    }
}
