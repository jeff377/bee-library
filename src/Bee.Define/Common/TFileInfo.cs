using System;

namespace Bee.Define
{
    /// <summary>
    /// 檔案資料。
    /// </summary>
    [Serializable]
    public class TFileInfo
    {
        private string _FIleName = string.Empty;
        private byte[] _FileBytes = null;

        /// <summary>
        /// 檔案名稱。
        /// </summary>
        public string FIleName
        {
            get { return _FIleName; }
            set { _FIleName = value; }
        }

        /// <summary>
        /// 檔案內容。
        /// </summary>
        public byte[] FileBytes
        {
            get { return _FileBytes; }
            set { _FileBytes = value; }
        }
    }
}
