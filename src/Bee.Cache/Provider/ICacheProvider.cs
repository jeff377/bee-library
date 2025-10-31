using System.Collections.Generic;

namespace Bee.Cache
{
    /// <summary>
    /// 快取提供者介面，定義快取操作的統一契約。
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// 判斷快取項目是否存在於快取中。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        bool Contains(string key);

        /// <summary>
        /// 將快取項目置入快取區中。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        /// <param name="value">要置入快取的物件。</param>
        /// <param name="policy">快取項目到期條件。</param>
        void Set(string key, object value, CacheItemPolicy policy);

        /// <summary>
        /// 從快取傳回項目。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        object Get(string key);

        /// <summary>
        /// 移除快取項目。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        /// <returns>傳回移除的快取項目,若快取項目不存在則傳回 null。</returns>
        object Remove(string key);

        /// <summary>
        /// 從快取物件移除指定百分比的快取項目。
        /// </summary>
        /// <param name="percent">移除項目的數目在快取項目總數中所佔的百分比。</param>
        /// <returns>從快取區中移除的項目數量。</returns>
        long Trim(int percent);

        /// <summary>
        /// 傳回快取中的快取項目總數。
        /// </summary>
        long GetCount();

        /// <summary>
        /// 取得所有快取的鍵值清單。
        /// </summary>
        /// <returns>快取鍵值的字串列舉。</returns>
        IEnumerable<string> GetAllKeys();
    }
}