using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 加密金鑰設定，用於設定檔中儲存加密資訊。
    /// </summary>
    [Serializable]
    [XmlType("SecurityKeySettings")]
    [Description("加密金鑰設定。")]
    [TreeNode("Security Keys")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SecurityKeySettings
    {
        /// <summary>
        /// 主金鑰來源。
        /// </summary>
        [Description("主金鑰來源。")]
        public MasterKeySource MasterKeySource { get; set; } = new MasterKeySource();

        /// <summary>
        /// API 傳輸金鑰（使用主金鑰加密儲存，base64 字串）。
        /// </summary>
        [Description("API 傳輸金鑰（使用主金鑰加密儲存，base64 字串）。")]
        public string ApiEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Cookie 金鑰（使用主金鑰加密儲存，base64 字串）。
        /// </summary>
        [Description("Cookie 金鑰（使用主金鑰加密儲存，base64 字串）")]
        public string CookieEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// 設定檔機敏資料加密金鑰（使用主金鑰加密儲存，base64 字串）。
        /// </summary>
        [Description("設定檔中機敏資料的加密金鑰（使用主金鑰加密儲存，base64 字串）。")]
        public string ConfigEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// 資料庫機敏欄位加密金鑰（使用主金鑰加密儲存，base64 字串）。
        /// </summary>
        [Description("資料庫中機敏欄位的加密金鑰（使用主金鑰加密儲存，base64 字串）。")]
        public string DatabaseEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }

}
