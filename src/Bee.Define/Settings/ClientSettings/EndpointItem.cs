using Bee.Base;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// 服務端點清單項目。
    /// </summary>
    [Serializable]
    [XmlType("EndpointItem")]
    [Description("服務端點清單項目。")]
    public class EndpointItem : CollectionItem
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public EndpointItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="name">服務端點名稱。</param>
        /// <param name="endpoint">服務端點位置，遠端連線為網址，近端連線為本地路徑。</param>
        public EndpointItem(string name, string endpoint)
        {
            Name = name;
            Endpoint = endpoint;
        }

        /// <summary>
        /// 服務端點名稱。
        /// </summary>
        [XmlAttribute]
        [Description("服務端點名稱。")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 服務端點位置，遠端連線為網址，近端連線為本地路徑。
        /// </summary>
        [XmlAttribute]
        [Description("服務端點位置，遠端連線為網址，近端連線為本地路徑。")]
        public string Endpoint { get; set; } = string.Empty;
    }
}
