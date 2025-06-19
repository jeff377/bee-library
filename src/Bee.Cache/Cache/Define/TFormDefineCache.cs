using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 表單定義快取。
    /// </summary>
    internal class TFormDefineCache : TKeyObjectCache<TFormDefine>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected override TCacheItemPolicy GetPolicy(string key)
        {
            // 程式代碼
            string progId = key;
            // 預設為相對時間 20 分鐘
            var policy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is TFileDefineProvider)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetFormDefineFilePath(progId) };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值為 [程式代碼]。</param>
        protected override TFormDefine CreateInstance(string key)
        {
            // 程式代碼
            string progId = key;
            return BackendInfo.DefineProvider.GetFormDefine(progId);
        }
    }
}
