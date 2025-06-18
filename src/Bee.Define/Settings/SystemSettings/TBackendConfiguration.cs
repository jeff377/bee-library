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
        /// 系統層級業務邏輯物件預設型別。
        /// </summary>
        [Category("System")]
        [Description("系統層級業務邏輯物件預設型別。")]
        [DefaultValue("")]
        public string SystemTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 表單層級業務邏輯物件預設型別。
        /// </summary>
        [Category("System")]
        [Description("表單層級業務邏輯物件預設型別。")]
        [DefaultValue("")]
        public string FormTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 業務邏輯物件提供者型別。
        /// </summary>
        [Category("System")]
        [Description("業務邏輯物件提供者型別。")]
        [DefaultValue("")]
        public string BusinessObjectProvider { get; set; } = string.Empty;

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
        [Category("API")]
        [Description("API KEY。")]
        [DefaultValue("")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            // 系統層級業務邏輯物件預設型別
            if (StrFunc.IsNotEmpty(SystemTypeName))
            {
                BackendInfo.SystemTypeName = SystemTypeName;
            }
            // 表單層級業務邏輯物件預設型別
            if (StrFunc.IsNotEmpty(FormTypeName))
            {
                BackendInfo.FormTypeName = FormTypeName;
            }
            // 指定業務邏輯物件提供者
            if (StrFunc.IsNotEmpty(BusinessObjectProvider))
            {
                BackendInfo.BusinessObjectProvider = BaseFunc.CreateInstance(BusinessObjectProvider) as IBusinessObjectProvider;
            }
            // 資料庫類型
            BackendInfo.DatabaseType = DatabaseType;
            // 預設資料庫編號
            BackendInfo.DatabaseID = DatabaseID;
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
