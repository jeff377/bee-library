using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 後端參數及環境設置。
    /// </summary>
    [Serializable]
    [XmlType("BackendConfiguration")]
    [Description("後端參數及環境設置。")]
    [TreeNode("後端")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class TBackendConfiguration
    {
        /// <summary>
        /// 系統層級商業邏輯物件預設型別。
        /// </summary>
        [Category("System")]
        [Description("系統層級商業邏輯物件預設型別。")]
        [DefaultValue("")]
        public string SystemTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 功能層級商業邏輯物件預設型別。
        /// </summary>
        [Category("System")]
        [Description("功能層級商業邏輯物件預設型別。")]
        [DefaultValue("")]
        public string BusinessTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        [Category("System")]
        [Description("資料庫類型。")]
        [DefaultValue(EDatabaseType.SQLServer)]
        public EDatabaseType DatabaseType { get; set; } = EDatabaseType.SQLServer;

        /// <summary>
        /// 預設資料庫編號。
        /// </summary>
        [Category("System")]
        [Description("預設資料庫編號。")]
        [DefaultValue("")]
        public string DatabaseID { get; set; } = string.Empty;

        /// <summary>
        /// API KEY。
        /// </summary>
        [Category("System")]
        [Description("API KEY。")]
        [DefaultValue("")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            // 系統層級商業邏輯物件預設型別
            if (StrFunc.IsNotEmpty(this.SystemTypeName))
                BackendInfo.SystemTypeName = this.SystemTypeName;
            // 功能層級商業邏輯物件預設型別
            if (StrFunc.IsNotEmpty(this.BusinessTypeName))
                BackendInfo.BusinessTypeName = this.BusinessTypeName;
            // 資料庫類型
            BackendInfo.DatabaseType = this.DatabaseType;
            // 預設資料庫編號
            BackendInfo.DatabaseID = this.DatabaseID;
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return "BackendConfiguration";
        }
    }
}
