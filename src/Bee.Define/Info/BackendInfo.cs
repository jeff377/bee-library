using System;

namespace Bee.Define
{
    /// <summary>
    /// 後端資訊，記錄伺服端在運行期間的參數及環境設置。
    /// </summary>
    public static class BackendInfo
    {
        private static IApiEncryptionKeyProvider _apiEncryptionKeyProvider = null;
        private static IBusinessObjectProvider _businessObjectProvider = null;
        private static IRepositoryProvider _repositoryProvider = null;
        private static ICacheDataSourceProvider _cacheDataSourceProvider = null;
        private static IDefineProvider _defineProvider = null;
        private static IAccessTokenValidationProvider _accessTokenValidationProvider = null;

        /// <summary>
        /// 定義資料路徑。
        /// </summary>
        public static string DefinePath { get; set; } = string.Empty;

        /// <summary>
        /// API 傳輸加密金鑰。
        /// </summary>
        public static byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Cookie 資料加密金鑰。
        /// </summary>
        public static byte[] CookieEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 設定檔機敏資料加密金鑰。
        /// </summary>
        public static byte[] ConfigEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 資料庫機敏欄位加密金鑰。
        /// </summary>
        public static byte[] DatabaseEncryptionKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public static DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// 預設資料庫識別。
        /// </summary>
        public static string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// 最大 DbCommand 逾時（秒）。0 表示不限制。
        /// </summary>
        public static int MaxDbCommandTimeout { get; set; } = 0;

        /// <summary>
        /// API 金鑰提供者，用於取得傳輸資料加解密所需的 AES+HMAC 金鑰。
        /// 支援共用金鑰與每次登入動態產生的 Session 金鑰。
        /// </summary>
        public static IApiEncryptionKeyProvider ApiEncryptionKeyProvider
        {
            get => _apiEncryptionKeyProvider;
            set => _apiEncryptionKeyProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 業務邏輯物件提供者，定義所有 BusinessObject 的取得方式。
        /// </summary>
        public static IBusinessObjectProvider BusinessObjectProvider
        {
            get => _businessObjectProvider;
            set => _businessObjectProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 資料儲存物件提供者，定義所有 Repository 的取得方式。
        /// </summary>
        public static IRepositoryProvider RepositoryProvider
        {
            get => _repositoryProvider;
            set => _repositoryProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 快取資料來源提供者。
        /// </summary>
        public static ICacheDataSourceProvider CacheDataSourceProvider
        {
            get => _cacheDataSourceProvider;
            set => _cacheDataSourceProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 定義資料提供者。
        /// </summary>
        public static IDefineProvider DefineProvider
        {
            get => _defineProvider;
            set => _defineProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// AccessToken 驗證提供者，用於驗證 AccessToken 的有效性。
        /// </summary>
        public static IAccessTokenValidationProvider AccessTokenValidationProvider
        {
            get => _accessTokenValidationProvider;
            set => _accessTokenValidationProvider = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
