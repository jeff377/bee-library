using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 主金鑰來源，包含來源類型與對應參數值。
    /// </summary>
    [Serializable]
    [XmlType("MasterKeySource")]
    [Description("主金鑰來源。")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class MasterKeySource
    {
        /// <summary>
        /// 主金鑰來源類型。
        /// </summary>
        [Description("主金鑰來源類型")]
        public MasterKeySourceType Type { get; set; } = MasterKeySourceType.File;

        /// <summary>
        /// 來源參數值：檔案路徑或環境變數名稱。
        /// 若為空白，將使用預設值。
        /// </summary>
        [Description("來源參數值，檔案路徑或環境變數名稱，若為空白將使用預設值")]
        [DefaultValue("")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 將主金鑰來源轉換為字串表示形式。
        /// </summary>
        public override string ToString()
        {
            return Type.ToString();
        }
    }

}
