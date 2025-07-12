using System;

namespace Bee.Base
{
    /// <summary>
    /// 將日誌輸出至 Console 的實作。
    /// </summary>
    public class ConsoleLogWriter : ILogWriter
    {
        /// <summary>
        /// 寫入一筆日誌記錄。
        /// </summary>
        /// <param name="entry">日誌內容。</param>
        public void Write(LogEntry entry)
        {
            switch (entry.EntryType)
            {
                case LogEntryType.Information:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogEntryType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogEntryType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine($"[{entry.EntryType}] [{entry.Source}] {entry.Message}");

            if (entry.Exception != null)
            {
                Console.WriteLine(entry.Exception.ToString());
            }

            Console.ResetColor();
        }
    }
}
