using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 加密金錀設定，用於設定檔中儲存加密資訊。
    /// </summary>
    [Serializable]
    [XmlType("SecurityKeySettings")]
    [Description("加密金錀設定。")]
    [TreeNode("加密金錀")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SecurityKeySettings
    {
        /// <summary>
        /// 主金錀來源。
        /// </summary>
        [Description("主金錀來源。")]
        public MasterKeySource MasterKeySource { get; set; } = new MasterKeySource();

        /// <summary>
        /// API 傳輸金錀（使用主金錀加密儲存，base64 字串）。
        /// </summary>
        public string ApiEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Cookie 金錀（使用主金錀加密儲存，base64 字串）。
        /// </summary>
        public string CookieEncryptionKey { get; set; } = string.Empty;
    }

}
