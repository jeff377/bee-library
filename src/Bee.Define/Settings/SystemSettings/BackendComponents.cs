using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 後端可替換組件的設定，定義各種後端服務的型別名稱。
    /// </summary>
    [Serializable]
    [XmlType("BackendComponents")]
    [Description("後端可替換組件的設定，定義各種後端服務的型別名稱")]
    [TreeNode("Components")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackendComponents
    {
        /// <summary>
        /// API encryption key provider type.
        /// </summary>
        [Category("Providers")]
        [Description("API encryption key provider type, defines how to obtain the API data encryption key.")]
        [DefaultValue(BackendDefaultTypes.ApiEncryptionKeyProvider)]
        public string ApiEncryptionKeyProvider { get; set; } = BackendDefaultTypes.ApiEncryptionKeyProvider;

        /// <summary>
        /// AccessToken validation provider type.
        /// </summary>
        [Category("Providers")]
        [Description("AccessToken validation provider type, used to validate the validity of AccessTokens.")]
        [DefaultValue(BackendDefaultTypes.AccessTokenValidationProvider)]
        public string AccessTokenValidationProvider { get; set; } = BackendDefaultTypes.AccessTokenValidationProvider;

        /// <summary>
        /// Business object provider type.
        /// </summary>
        [Category("Providers")]
        [Description("Business object provider type, defines how to obtain all BusinessObjects.")]
        [DefaultValue(BackendDefaultTypes.BusinessObjectProvider)]
        public string BusinessObjectProvider { get; set; } = BackendDefaultTypes.BusinessObjectProvider;

        /// <summary>
        /// Cache data source provider type.
        /// </summary>
        [Category("Providers")]
        [Description("Cache data source provider type, defines the source of cached data (such as preloaded definition data).")]
        [DefaultValue(BackendDefaultTypes.CacheDataSourceProvider)]
        public string CacheDataSourceProvider { get; set; } = BackendDefaultTypes.CacheDataSourceProvider;

        /// <summary>
        /// Define storage type.
        /// </summary>
        [Category("Define")]
        [Description("Define storage type, specifies how to load system definition files (e.g., file, database, etc.).")]
        [DefaultValue(BackendDefaultTypes.DefineStorage)]
        public string DefineStorage { get; set; } = BackendDefaultTypes.DefineStorage;

        /// <summary>
        /// Define access type.
        /// </summary>
        [Category("Define")]
        [Description("Define access type.")]
        [DefaultValue(BackendDefaultTypes.DefineAccess)]
        public string DefineAccess { get; set; } = BackendDefaultTypes.DefineAccess;

        /// <summary>
        /// Session info service type.
        /// </summary>
        [Category("Service")]
        [Description("Session info service type.")]
        [DefaultValue(BackendDefaultTypes.SessionInfoService)]
        public string SessionInfoService { get; set; } = BackendDefaultTypes.SessionInfoService;

        /// <summary>
        /// Unified access service type for commonly used enterprise business objects.
        /// </summary>
        [Category("Service")]
        [Description("Unified access service type for commonly used enterprise business objects.")]
        [DefaultValue(BackendDefaultTypes.EnterpriseObjectService)]
        public string EnterpriseObjectService { get; set; } = BackendDefaultTypes.EnterpriseObjectService;

        /// <summary>
        /// System level repository provider type.
        /// </summary>
        [Category("Repository")]
        [Description("System level Repository provider type.")]
        [DefaultValue(BackendDefaultTypes.SystemRepositoryProvider)]
        public string SystemRepositoryProvider { get; set; } = BackendDefaultTypes.SystemRepositoryProvider;

        /// <summary>
        /// Form level repository provider type.
        /// </summary>
        [Category("Repository")]
        [Description("Form level Repository provider type.")]
        [DefaultValue(BackendDefaultTypes.FormRepositoryProvider)]
        public string FormRepositoryProvider { get; set; } = BackendDefaultTypes.FormRepositoryProvider;

        /// <summary>
        /// 套用目前設定，將指定的型別實例化並指派至 <see cref="BackendInfo"/>。
        /// </summary>
        public void Apply()
        {
            BackendInfo.ApiEncryptionKeyProvider = CreateOrDefault<IApiEncryptionKeyProvider>(ApiEncryptionKeyProvider, BackendDefaultTypes.ApiEncryptionKeyProvider);
            BackendInfo.AccessTokenValidationProvider = CreateOrDefault<IAccessTokenValidationProvider>(AccessTokenValidationProvider, BackendDefaultTypes.AccessTokenValidationProvider);
            BackendInfo.BusinessObjectProvider = CreateOrDefault<IBusinessObjectProvider>(BusinessObjectProvider, BackendDefaultTypes.BusinessObjectProvider);
            BackendInfo.CacheDataSourceProvider = CreateOrDefault<ICacheDataSourceProvider>(CacheDataSourceProvider, BackendDefaultTypes.CacheDataSourceProvider);
            BackendInfo.DefineStorage = CreateOrDefault<IDefineStorage>(DefineStorage, BackendDefaultTypes.DefineStorage);
            BackendInfo.DefineAccess = CreateOrDefault<IDefineAccess>(DefineAccess, BackendDefaultTypes.DefineAccess);
            BackendInfo.SessionInfoService = CreateOrDefault<ISessionInfoService>(SessionInfoService, BackendDefaultTypes.SessionInfoService);
            BackendInfo.EnterpriseObjectService = CreateOrDefault<IEnterpriseObjectService>(EnterpriseObjectService, BackendDefaultTypes.EnterpriseObjectService);
        }

        /// <summary>
        /// 建立指定型別的實例，若 <paramref name="configured"/> 為空則使用 <paramref name="fallback"/>。
        /// </summary>
        /// <typeparam name="T">要建立的型別，必須為 class。</typeparam>
        /// <param name="configured">組態指定的型別名稱。</param>
        /// <param name="fallback">預設型別名稱。</param>
        /// <returns>型別 <typeparamref name="T"/> 的實例，若建立失敗則回傳 null。</returns>
        private static T CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return BaseFunc.CreateInstance(typeName) as T;
        }
    }
}
