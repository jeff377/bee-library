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
    public class BackendConfiguration
    {
        /// <summary>
        /// 業務邏輯物件提供者型別，定義所有 BusinessObject 的取得方式。
        /// </summary>
        [Category("System")]
        [Description("業務邏輯物件提供者型別，定義所有 BusinessObject 的取得方式。")]
        [DefaultValue("")]
        public string BusinessObjectProvider { get; set; } = string.Empty;

        /// <summary>
        /// 資料儲存物件提供者型別，定義所有 Repository 的取得方式。
        /// </summary>
        [Category("System")]
        [Description("資料儲存物件提供者型別，定義所有 Repository 的取得方式。")]
        [DefaultValue("")]
        public string RepositoryProvider { get; set; } = string.Empty;

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        [Category("System")]
        [Description("資料庫類型。")]
        [DefaultValue(DatabaseType.SQLServer)]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

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
        /// API 專用的加密金鑰，以 base64 編碼的字串表示。
        /// </summary>
        public string ApiEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            // 指定業務邏輯物件提供者
            if (StrFunc.IsNotEmpty(BusinessObjectProvider))
            {
                BackendInfo.BusinessObjectProvider = BaseFunc.CreateInstance(BusinessObjectProvider) as IBusinessObjectProvider;
            }
            // 指定資料儲存物件提供者型別
            if (StrFunc.IsNotEmpty(RepositoryProvider))
            {
                BackendInfo.RepositoryProvider = BaseFunc.CreateInstance(RepositoryProvider) as IRepositoryProvider;
            }
            // 資料庫類型
            BackendInfo.DatabaseType = DatabaseType;
            // 預設資料庫編號
            BackendInfo.DatabaseID = DatabaseID;
            // 取得 API 專用的加密金鑰
            if (StrFunc.IsNotEmpty(ApiEncryptionKey))
            {
                BackendInfo.ApiEncryptionKeySet = new AesHmacKeySet(ApiEncryptionKey);
            }
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
