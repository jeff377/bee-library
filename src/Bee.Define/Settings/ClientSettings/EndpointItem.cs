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
        private string _Name = string.Empty;
        private string _Endpoint = string.Empty;

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
            _Name = name;
            _Endpoint = endpoint;
        }

        /// <summary>
        /// 服務端點名稱。
        /// </summary>
        [XmlAttribute]
        [Description("服務端點名稱。")]
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        /// <summary>
        /// 服務端點位置，遠端連線為網址，近端連線為本地路徑。
        /// </summary>
        [XmlAttribute]
        [Description("服務端點位置，遠端連線為網址，近端連線為本地路徑。")]
        public string Endpoint
        {
            get { return _Endpoint; }
            set { _Endpoint = value; }
        }
    }
}
