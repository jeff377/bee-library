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

    }
}
