using System;

namespace Bee.Base
{
    /// <summary>
    /// FTP 項目類型的列舉。
    /// </summary>
    public enum EFtpItemType
    {
        /// <summary>
        /// 檔案。
        /// </summary>
        File,
        /// <summary>
        /// 目錄。
        /// </summary>
        Directory
    }

    /// <summary>
    /// 表示 FTP 項目的類別，包括名稱、類型、大小和最後修改時間。
    /// </summary>
    public class TFtpItem
    {
        /// <summary>
        /// 項目名稱。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 項目類型。
        /// </summary>
        public EFtpItemType Type { get; set; }

        /// <summary>
        /// 項目大小（目錄大小為 0）。
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 項目最後修改時間。
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{Type} - {Name}";
        }
    }
}
