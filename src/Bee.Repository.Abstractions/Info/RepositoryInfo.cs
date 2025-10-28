using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// 提供存取系統儲存庫與表單儲存庫的靜態介面。
    /// </summary>
    public static class RepositoryInfo
    {
        static RepositoryInfo()
        {
            if (SysInfo.IsSingleFile) { return; }
            if (BackendInfo.DefineAccess == null)
                throw new InvalidOperationException("BackendInfo.DefineAccess cannot be null. Please ensure the backend configuration is properly initialized.");

            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            Initialize(settings.BackendConfiguration);
        }

        /// <summary>
        /// 取得或設定系統儲存庫提供者。
        /// </summary>
        public static ISystemRepositoryProvider SystemProvider { get; set; }

        /// <summary>
        /// 取得或設定表單儲存庫提供者。
        /// </summary>
        public static IFormRepositoryProvider FormProvider { get; set; }

        /// <summary>
        /// 初始化。
        /// </summary>
        private static void Initialize(BackendConfiguration configuration)
        {
            var components = configuration.Components;
            // 設定系統儲存庫提供者
            SystemProvider = CreateOrDefault<ISystemRepositoryProvider>
                (components.SystemRepositoryProvider, BackendDefaultTypes.SystemRepositoryProvider);
            // 設定表單儲存庫提供者
            FormProvider = CreateOrDefault<IFormRepositoryProvider>
                (components.FormRepositoryProvider, BackendDefaultTypes.FormRepositoryProvider);
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