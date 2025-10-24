using Bee.Api.Core;
using Bee.Cache;
using Bee.Db;
using Bee.Define;
using Bee.Repository;
using Bee.Repository.Abstractions;

namespace ApiService.Extensions
{
    /// <summary>
    /// 後端程式擴充方法。
    /// </summary>
    public static class BackendExtensions
    {
        /// <summary>
        /// 後端程式初始化。
        /// </summary>
        /// <param name="app">The Microsoft.AspNetCore.Builder.IApplicationBuilder to add the middleware to.</param>
        /// <param name="configuration"></param>
        public static IApplicationBuilder BackendInitialize(this IApplicationBuilder app, IConfiguration configuration)
        {
            // 從組態載入 DefinePath
            var definePath = configuration["DefinePath"]
                ?? throw new InvalidOperationException("DefinePath 未設定");

            // 將相對路徑轉為絕對路徑（以執行目錄為基準）
            var absolutePath = Path.GetFullPath(definePath, AppContext.BaseDirectory);

            // 確保目錄存在
            if (!Directory.Exists(absolutePath))
                throw new DirectoryNotFoundException($"DefinePath 指定的目錄不存在：{absolutePath}");

            BackendInfo.DefinePath = absolutePath;
            // 系統設定初始化
            var settings = CacheFunc.GetSystemSettings();
            settings.Initialize();
            // 儲存庫初始化
            RepositoryInfo.Initialize(settings.BackendConfiguration);
            // 初始化 API 服務選項，設定序列化器、壓縮器與加密器的實作
            ApiServiceOptions.Initialize(settings.CommonConfiguration.ApiPayloadOptions);
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            return app;
        }
    }
}
