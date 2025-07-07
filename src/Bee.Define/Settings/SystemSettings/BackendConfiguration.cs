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
        /// 加密金錀設定。
        /// </summary>
        [Category("Security")]
        [Description("加密金錀設定。")]
        public SecurityKeySettings SecurityKeySettings { get; set; } = new SecurityKeySettings();

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
            // 初始化金錀
            Initialize(SecurityKeySettings);
        }

        /// <summary>
        /// 初始化金錀。
        /// </summary>
        /// <param name="settings">金錀設定。</param>
        private static void Initialize(SecurityKeySettings settings)
        {
            byte[] masterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource);
            AesCbcHmacKeyGenerator.FromCombinedKey(masterKey, out var aesKey, out var hmacKey);

            // 解密 API 金錀，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.ApiEncryptionKey))
            {
                byte[] bytes = Convert.FromBase64String(settings.ApiEncryptionKey);
                BackendInfo.ApiEncryptionKey = AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
            }

            // 解密 Cookie 金錀，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.CookieEncryptionKey))
            {
                byte[] bytes = Convert.FromBase64String(settings.CookieEncryptionKey);
                BackendInfo.CookieEncryptionKey = AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
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
