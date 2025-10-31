using System;
using System.Collections.Generic;
using System.Text;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 提供存取快取提供者的靜態介面。
    /// </summary>
    public static class CacheInfo
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        static CacheInfo()
        {
            if (SysInfo.IsSingleFile) { return; }
            if (BackendInfo.DefineAccess == null)
                throw new InvalidOperationException("BackendInfo.DefineAccess cannot be null. Please ensure the backend configuration is properly initialized.");

            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            Initialize(settings.BackendConfiguration);
        }
        /// <summary>
        /// 快取提供者。
        /// </summary>
        public static ICacheProvider Provider { get; set; } = new MemoryCacheProvider();

        /// <summary>
        /// 初始化。
        /// </summary>
        private static void Initialize(BackendConfiguration configuration)
        {
            var components = configuration.Components;
            // 快取提供者
            Provider = CreateOrDefault<ICacheProvider>
                (components.CacheProvider, BackendDefaultTypes.CacheProvider);
        }

        /// <summary>
        /// 建立指定型別的實例，若 <paramref name="configured"/> 為空則使用 <paramref name="fallback"/>。
        /// </summary>
        /// <param name="configured">組態指定的型別名稱。</param>
        /// <param name="fallback">預設型別名稱。</param>
        private static T CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return BaseFunc.CreateInstance(typeName) as T;
        }
    }
}
