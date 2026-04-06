using Bee.Cache;
using Bee.Define;
using Bee.Define.Forms;
using Bee.Define.Storage;

namespace Bee.Cache.Define
{
    /// <summary>
    /// 表單定義快取。
    /// </summary>
    internal class FormSchemaCache : KeyObjectCache<FormSchema>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // 程式代碼
            string progId = key;
            // 預設為相對時間 20 分鐘
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineStorage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetFormSchemaFilePath(progId) };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值為 [程式代碼]。</param>
        protected override FormSchema CreateInstance(string key)
        {
            // 程式代碼
            string progId = key;
            return BackendInfo.DefineStorage.GetFormSchema(progId);
        }
    }
}
