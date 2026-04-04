using Bee.Define.Settings;
using Bee.Base;
using Bee.Define;
using System;

namespace Bee.Cache
{
    /// <summary>
    /// 提供存取快取提供者的靜態介面。
    /// </summary>
    /// <remarks>
    /// 此類別負責初始化和管理快取提供者的實例，並提供一個靜態屬性 `Provider` 來存取該實例。
    /// 它會根據後端組態來決定使用哪一個快取提供者，若未指定則使用預設的 `MemoryCacheProvider`。
    /// </remarks>
    public static class CacheInfo
    {
        /// <summary>
        /// 靜態建構函式，用於初始化快取提供者。
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
        /// 快取提供者的靜態屬性。
        /// </summary>
        /// <value>
        /// 預設為 `MemoryCacheProvider`，但可以根據後端組態進行覆寫。
        /// </value>
        public static ICacheProvider Provider { get; set; } = new MemoryCacheProvider();

        /// <summary>
        /// 初始化快取提供者。
        /// </summary>
        /// <param name="configuration">後端組態資訊。</param>
        private static void Initialize(BackendConfiguration configuration)
        {
            var components = configuration.Components;
            // 根據組態或預設值建立快取提供者
            Provider = CreateOrDefault<ICacheProvider>
                (components.CacheProvider, BackendDefaultTypes.CacheProvider);
        }

        /// <summary>
        /// 建立指定型別的實例，若 <paramref name="configured"/> 為空則使用 <paramref name="fallback"/>。
        /// </summary>
        /// <typeparam name="T">要建立的實例型別。</typeparam>
        /// <param name="configured">組態中指定的型別名稱。</param>
        /// <param name="fallback">若未指定型別時使用的預設型別名稱。</param>
        /// <returns>建立的實例，或 null（若型別無法建立）。</returns>
        private static T CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return BaseFunc.CreateInstance(typeName) as T;
        }
    }
}
