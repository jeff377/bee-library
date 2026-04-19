using System.Text;
using System.Text.RegularExpressions;

namespace Bee.Base
{
    /// <summary>
    /// Utility library for file access operations.
    /// </summary>
    public static class FileFunc
    {
        #region File Methods

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static void FileDelele(string filePath)
        {
            if (FileFunc.FileExists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// Reads a file and returns its contents as a byte array.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static byte[] FileToBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// Writes a byte array to the specified file.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="filePath">The file path.</param>
        public static void BytesToFile(byte[] bytes, string filePath)
        {
            // Verify the directory exists; create it if not
            DirectoryCheck(filePath, true);
            // Write binary data to the file
            File.WriteAllBytes(filePath, bytes);
        }

        /// <summary>
        /// Opens a file as a stream.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static Stream FileToStream(string filePath)
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        /// <summary>
        /// Writes a stream to the specified file.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="filePath">The file path.</param>
        public static void StreamToFile(Stream stream, string filePath)
        {
            // Verify the directory exists; create it if not
            DirectoryCheck(filePath, true);

            // Set the stream position to the beginning
            stream.Seek(0, SeekOrigin.Begin);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.CopyTo(fileStream);
            }
        }

        /// <summary>
        /// Writes text to a file and closes it. Overwrites the file if it exists. Default encoding is UTF-8.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="contents">The string to write to the file.</param>
        public static void FileWriteText(string filePath, string contents)
        {
            // UTF8Encoding(false) is UTF-8 encoding without a byte order mark (BOM)
            FileWriteText(filePath, contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// Writes text to a file and closes it. Overwrites the file if it exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="encoding">The encoding to use.</param>
        public static void FileWriteText(string filePath, string contents, Encoding encoding)
        {
            // Verify the directory exists; create it if not
            DirectoryCheck(filePath, true);

            File.WriteAllText(filePath, contents, encoding);
        }

        /// <summary>
        /// Reads the contents of a text file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static string FileReadText(string filePath)
        {
            if (!FileFunc.FileExists(filePath))
                return string.Empty;
            else
                return File.ReadAllText(filePath);
        }

        #endregion

        #region Directory Methods

        /// <summary>
        /// Determines whether the specified directory exists.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Creates the specified directory.
        /// </summary>
        /// <param name="path">The path to create.</param>
        public static void DirectoryCreate(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Checks whether the specified directory exists; creates it if it does not.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="isFilePath">Whether the path is a file path; if so, the directory is extracted first before checking.</param>
        public static void DirectoryCheck(string path, bool isFilePath = false)
        {
            string? sPath;

            // Get the directory path
            sPath = (isFilePath) ? GetDirectory(path) : path;
            // Check if directory exists; create if not
            if (sPath != null && !DirectoryExists(sPath))
                DirectoryCreate(sPath);
        }

        #endregion

        #region Path Methods

        /// <summary>
        /// Determines whether the specified input is a local path.
        /// </summary>
        /// <param name="input">The input path.</param>
        public static bool IsLocalPath(string input)
        {
            // Check whether it is a Windows path or UNC network path
            string pattern = @"^([a-zA-Z]:\\|\\\\)";
            return Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Determines whether the specified path is an absolute (rooted) path.
        /// </summary>
        /// <param name="path">The path string to check.</param>
        /// <returns>True if the path is absolute; otherwise, false.</returns>
        public static bool IsPathRooted(string path)
        {
            return Path.IsPathRooted(path);
        }

        /// <summary>
        /// Gets the directory portion of the specified path.
        /// </summary>
        /// <param name="path">The path string.</param>
        public static string? GetDirectory(string path)
        {
           return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Gets the parent directory of the specified path.
        /// </summary>
        /// <param name="path">The path string.</param>
        public static string GetParentDirectory(string path)
        {
            return Directory.GetParent(path)!.FullName;
        }

        /// <summary>
        /// Gets the file name and extension from the specified path.
        /// </summary>
        /// <param name="path">The path string.</param>
        /// <param name="isExtension">Whether to include the file extension.</param>
        public static string GetFileName(string path, bool isExtension = true)
        {
            if (isExtension)
                return Path.GetFileName(path);
            else
                return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Gets the file extension from the specified file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        /// <summary>
        /// Combines an array of path strings into a single path.
        /// </summary>
        /// <param name="paths">An array of path components.</param>
        /// <remarks>Returns the combined path.</remarks>
        public static string PathCombine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <summary>
        /// Gets the application base path, or a sub-path beneath it.
        /// </summary>
        /// <param name="subPath">The sub-path to append.</param>
        public static string GetAppPath(string subPath ="")
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (StrFunc.IsNotEmpty(subPath))
                path = PathCombine(path, subPath);
            return path;
        }

        /// <summary>
        /// Gets the application's private assembly directory.
        /// </summary>
        public static string GetAssemblyPath()
        {
            // Get the assembly path
            if (StrFunc.IsEmpty(AppDomain.CurrentDomain.RelativeSearchPath))
                return AppDomain.CurrentDomain.BaseDirectory;
            else
                return AppDomain.CurrentDomain.RelativeSearchPath!;
        }

        #endregion
    }
}
