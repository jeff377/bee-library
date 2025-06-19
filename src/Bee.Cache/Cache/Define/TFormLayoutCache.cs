using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 表單版面配置快取。
    /// </summary>
    internal class TFormLayoutCache : TKeyObjectCache<TFormLayout>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected override TCacheItemPolicy GetPolicy(string key)
        {
            // 表單版面代碼
            string layoutId = key;
            // 預設為相對時間 20 分鐘
            var policy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is TFileDefineProvider)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetFormLayoutFilePath(layoutId) };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值為 [表單版面代碼]。</param>
        protected override TFormLayout CreateInstance(string key)
        {
            // 表單版面代碼
            string layoutId = key;
            return BackendInfo.DefineProvider.GetFormLayout(layoutId);
        }
    }
}
