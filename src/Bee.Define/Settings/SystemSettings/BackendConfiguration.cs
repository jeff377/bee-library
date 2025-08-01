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
    [TreeNode("Backend")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackendConfiguration
    {
        /// <summary>
        /// API 加密金鑰提供者型別。
        /// </summary>
        [Category("Providers")]
        [Description("API 加密金鑰提供者型別，定義 API 傳輸資料加密金鑰的取得方式。")]
        [DefaultValue(DefaultProviderTypes.ApiEncryptionKeyProvider)]
        public string ApiEncryptionKeyProvider { get; set; } = DefaultProviderTypes.ApiEncryptionKeyProvider;

        /// <summary>
        /// 業務邏輯物件提供者型別。
        /// </summary>
        [Category("Providers")]
        [Description("業務邏輯物件提供者型別，定義所有 BusinessObject 的取得方式。")]
        [DefaultValue(DefaultProviderTypes.BusinessObjectProvider)]
        public string BusinessObjectProvider { get; set; } = DefaultProviderTypes.BusinessObjectProvider;

        /// <summary>
        /// 資料儲存物件提供者型別。
        /// </summary>
        [Category("Providers")]
        [Description("資料儲存物件提供者型別，定義所有 Repository 的取得方式。")]
        [DefaultValue(DefaultProviderTypes.RepositoryProvider)]
        public string RepositoryProvider { get; set; } = DefaultProviderTypes.RepositoryProvider;

        /// <summary>
        /// 定義資料提供者型別。
        /// </summary>
        [Category("Providers")]
        [Description("定義資料提供者型別，定義系統定義檔的載入方式（如檔案、資料庫等）。")]
        [DefaultValue(DefaultProviderTypes.DefineProvider)]
        public string DefineProvider { get; set; } = DefaultProviderTypes.DefineProvider;

        /// <summary>
        /// 快取資料來源提供者型別。
        /// </summary>
        [Category("Providers")]
        [Description("快取資料來源提供者型別，定義資料快取來源（如預先載入定義資料）。")]
        [DefaultValue(DefaultProviderTypes.CacheDataSourceProvider)]
        public string CacheDataSourceProvider { get; set; } = DefaultProviderTypes.CacheDataSourceProvider;

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        [Category("Database")]
        [Description("資料庫類型。")]
        [DefaultValue(DatabaseType.SQLServer)]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// 預設資料庫編號。
        /// </summary>
        [Category("Database")]
        [Description("預設資料庫編號。")]
        [DefaultValue("")]
        public string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// API KEY。
        /// </summary>
        [Category("API")]
        [Description("API KEY。")]
        [DefaultValue("")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 加密金鑰設定。
        /// </summary>
        [Category("Security")]
        [Description("加密金鑰設定。")]
        [Browsable(false)]
        public SecurityKeySettings SecurityKeySettings { get; set; } = new SecurityKeySettings();

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            // 資料庫類型
            BackendInfo.DatabaseType = DatabaseType;
            // 預設資料庫編號
            BackendInfo.DatabaseId = DatabaseId;

            // 指定 API 加密金鑰提供者型別
            BackendInfo.ApiEncryptionKeyProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(ApiEncryptionKeyProvider)
                    ? DefaultProviderTypes.ApiEncryptionKeyProvider
                    : ApiEncryptionKeyProvider
            ) as IApiEncryptionKeyProvider;

            // 指定業務邏輯物件提供者
            BackendInfo.BusinessObjectProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(BusinessObjectProvider)
                    ? DefaultProviderTypes.BusinessObjectProvider
                    : BusinessObjectProvider
            ) as IBusinessObjectProvider;

            // 指定資料儲存物件提供者型別
            BackendInfo.RepositoryProvider = BaseFunc.CreateInstance(
                 string.IsNullOrWhiteSpace(RepositoryProvider)
                     ? DefaultProviderTypes.RepositoryProvider
                     : RepositoryProvider
             ) as IRepositoryProvider;

            // 指定快取資料來源提供者型別
            BackendInfo.CacheDataSourceProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(CacheDataSourceProvider)
                    ? DefaultProviderTypes.CacheDataSourceProvider
                    : CacheDataSourceProvider
            ) as ICacheDataSourceProvider;

            // 指定定義資料提供者型別
            BackendInfo.DefineProvider = BaseFunc.CreateInstance(
                string.IsNullOrWhiteSpace(DefineProvider)
                    ? DefaultProviderTypes.DefineProvider
                    : DefineProvider
            ) as IDefineProvider;

            // 初始化金鑰
            InitializeSecurityKeys();
        }

        /// <summary>
        /// 初始化金鑰。
        /// </summary>
        /// <param name="autoCreate">若主金鑰不存在，是否自動建立。</param>
        public void InitializeSecurityKeys(bool autoCreate = false)
        {
            var settings = SecurityKeySettings;
            byte[] masterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource, autoCreate);

            // 解密 API 加密金鑰，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.ApiEncryptionKey))
            {
                BackendInfo.ApiEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ApiEncryptionKey);
            }

            // 解密 Cookie 金鑰，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.CookieEncryptionKey))
            {
                BackendInfo.CookieEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.CookieEncryptionKey);
            }

            // 解密設定檔金鑰，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.ConfigEncryptionKey))
            {
                BackendInfo.ConfigEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.ConfigEncryptionKey);
            }

            // 解密資料庫金鑰，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.DatabaseEncryptionKey))
            {
                BackendInfo.DatabaseEncryptionKey = EncryptionKeyProtector.DecryptEncryptedKey(masterKey, settings.DatabaseEncryptionKey);
            }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
