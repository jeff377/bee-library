using Bee.Base;
using Bee.Define;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// 提供存取系統儲存庫與表單儲存庫的靜態介面。
    /// </summary>
    public static class RepositoryInfo
    {
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
        public static void Initialize(BackendConfiguration configuration)
        {
            // 設定系統儲存庫提供者
            SystemProvider = BaseFunc.CreateInstance(
                 string.IsNullOrWhiteSpace(configuration.SystemRepositoryProvider)
                     ? DefaultProviderTypes.SystemRepositoryProvider
                     : configuration.SystemRepositoryProvider
             ) as ISystemRepositoryProvider;

            // 設定表單儲存庫提供者
            FormProvider = BaseFunc.CreateInstance(
                 string.IsNullOrWhiteSpace(configuration.FormRepositoryProvider)
                     ? DefaultProviderTypes.FormRepositoryProvider
                     : configuration.FormRepositoryProvider
             ) as IFormRepositoryProvider;
        }
    }
}