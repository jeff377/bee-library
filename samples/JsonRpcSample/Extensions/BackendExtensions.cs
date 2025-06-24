using Bee.Api.Core;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

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
            BackendInfo.DefinePath = configuration["DefinePath"]
                ?? throw new InvalidOperationException("DefinePath 未設定");
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // ⚠️ 注意：BinaryFormatter 已於 .NET 8 停用，僅限於相容性用途，建議移除或改為 MessagePack。
            // .NET 8 預設停用 BinaryFormatter，需手動啟用
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

            // 系統設定初始化
            var settings = CacheFunc.GetSystemSettings();
            settings.Initialize();
            // 初始化 API 服務選項，設定序列化器、壓縮器與加密器的實作
            ApiServiceOptions.Initialize(settings.CommonConfiguration.ApiPayloadOptions);

            return app;
        }
    }
}
