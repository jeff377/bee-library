using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 服務端點清單項目集合。
    /// </summary>
    [Serializable]
    public class TEndpointItemCollection : TCollectionBase<TEndpointItem>
    {
        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="name">服務端點名稱。</param>
        /// <param name="endpoint">服務端點位置，遠端連線為網址，近端連線為本地路徑。</param>
        public TEndpointItem Add(string name, string endpoint)
        {
            TEndpointItem oItem;

            oItem = new TEndpointItem(name, endpoint);
            this.Add(oItem);
            return oItem;
        }
    }
}
