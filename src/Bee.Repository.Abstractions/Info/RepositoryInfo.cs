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
    }
}