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
        public string ApiEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Cookie 金鑰（使用主金鑰加密儲存，base64 字串）。
        /// </summary>
        public string CookieEncryptionKey { get; set; } = string.Empty;
    }

}
