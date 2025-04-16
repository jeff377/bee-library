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
            TCacheItemPolicy oPolicy;
            string sProgID;

            // 程式代碼
            sProgID = key;
            // 預設為相對時間 20 分鐘
            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is TFileDefineProvider)
                oPolicy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetFormDefineFilePath(sProgID) };
            return oPolicy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值為 [程式代碼]。</param>
        protected override TFormDefine CreateInstance(string key)
        {
            TFormDefine oValue;
            string sProgID;

            // 程式代碼
            sProgID = key;
            oValue = BackendInfo.DefineProvider.GetFormDefine(sProgID);
            return oValue;
        }
    }
}
