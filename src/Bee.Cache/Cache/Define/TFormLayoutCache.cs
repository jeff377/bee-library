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
            TCacheItemPolicy oPolicy;
            string sLayoutID;

            // 版面代碼
            sLayoutID = key;
            // 預設為相對時間 20 分鐘
            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is TFileDefineProvider)
                oPolicy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetFormLayoutFilePath(sLayoutID) };
            return oPolicy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值為 [版面代碼]。</param>
        protected override TFormLayout CreateInstance(string key)
        {
            TFormLayout oValue;
            string sLayoutID;

            // 版面代碼
            sLayoutID = key;
            oValue = BackendInfo.DefineProvider.GetFormLayout(sLayoutID);
            return oValue;
        }
    }
}
