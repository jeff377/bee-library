using System.Text;
using System.Text.RegularExpressions;

namespace Bee.Base
{
    /// <summary>
    /// Framework-level file utilities not covered by BCL <see cref="System.IO.File"/> /
    /// <see cref="System.IO.Path"/>: text I/O with UTF-8 no-BOM default and auto-create
    /// directory, missing-file-as-empty read, Windows local-path detection, and assembly
    /// path resolution.
    /// </summary>
    public static class FileUtilities
    {
        /// <summary>
        /// Writes text to a file and closes it. Overwrites the file if it exists. Uses UTF-8
        /// without byte order mark. Creates the target directory if it does not exist.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="contents">The string to write to the file.</param>
        public static void FileWriteText(string filePath, string contents)
        {
            // UTF8Encoding(false) is UTF-8 encoding without a byte order mark (BOM)
            FileWriteText(filePath, contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// Writes text to a file and closes it. Overwrites the file if it exists. Creates
        /// the target directory if it does not exist.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="encoding">The encoding to use.</param>
        public static void FileWriteText(string filePath, string contents, Encoding encoding)
        {
            DirectoryCheck(filePath, true);
            File.WriteAllText(filePath, contents, encoding);
        }

        /// <summary>
        /// Writes text to a file so a reader never observes a half-written file: the content goes
        /// to a temporary file in the same directory, which then replaces the target in one
        /// filesystem operation. Uses UTF-8 without byte order mark and creates the target
        /// directory if it does not exist.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <exception cref="IOException">
        /// Thrown when the replace step fails. On Windows this includes another process holding the
        /// target open without delete sharing, which is a normal outcome when several processes
        /// write the same file at once — a caller writing idempotent content should treat it as
        /// "someone else got there first" rather than as an error.
        /// </exception>
        /// <remarks>
        /// The replace is <see cref="File.Move(string, string, bool)"/>, which maps to
        /// <c>rename(2)</c> on Unix and <c>MoveFileEx(MOVEFILE_REPLACE_EXISTING)</c> on Windows;
        /// both replace atomically within one volume, and the temporary file is created beside the
        /// target so that holds. Note the two-argument <c>File.Move</c> would throw instead of
        /// replacing — the overwrite flag is what makes this work at all.
        /// </remarks>
        public static void FileWriteTextAtomic(string filePath, string contents)
        {
            DirectoryCheck(filePath, true);

            string directory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".";
            string tempPath = Path.Combine(directory, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                File.WriteAllText(tempPath, contents, new UTF8Encoding(false));
                File.Move(tempPath, filePath, overwrite: true);
            }
            catch
            {
                // Best effort: a leftover temporary file beside the target is confusing, and the
                // caller is about to see the original exception either way.
                try { File.Delete(tempPath); } catch (IOException) { /* nothing further to try */ }
                throw;
            }
        }

        /// <summary>
        /// Reads the contents of a text file. Returns empty string when the file does not exist.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static string FileReadText(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;
            return File.ReadAllText(filePath);
        }

        /// <summary>
        /// Determines whether the specified input is a local Windows path (drive letter or UNC).
        /// </summary>
        /// <param name="input">The input path.</param>
        public static bool IsLocalPath(string input)
        {
            // Check whether it is a Windows path or UNC network path
            string pattern = @"^([a-zA-Z]:\\|\\\\)";
            return Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Gets the application's private assembly directory. Falls back to the application
        /// base directory when no relative search path is configured.
        /// </summary>
        public static string GetAssemblyPath()
        {
            if (StringUtilities.IsEmpty(AppDomain.CurrentDomain.RelativeSearchPath))
                return AppDomain.CurrentDomain.BaseDirectory;
            return AppDomain.CurrentDomain.RelativeSearchPath!;
        }

        /// <summary>
        /// Checks whether the specified directory exists; creates it if it does not.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="isFilePath">When <c>true</c>, the directory portion is extracted from a file path first.</param>
        private static void DirectoryCheck(string path, bool isFilePath = false)
        {
            string? dir = isFilePath ? Path.GetDirectoryName(path) : path;
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
