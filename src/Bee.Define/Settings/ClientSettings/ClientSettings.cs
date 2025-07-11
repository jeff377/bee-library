using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 用戶端設定。
    /// </summary>
    [Serializable]
    [XmlType("ClientSettings")]
    [Description("用戶端設定。")]
    public class ClientSettings : IObjectSerializeFile
    {
        private string _ObjectFilePath = string.Empty;
        private SerializeState _SerializeState = SerializeState.None;
        private string _Endpoint = string.Empty;
        private EndpointItemCollection _EndpointItems = new EndpointItemCollection();

        #region 建構函式

        /// <summary>
        /// 建構函式 
        /// </summary>
        public ClientSettings()
        {
            CreateTime = DateTime.Now;
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState
        {
            get { return _SerializeState; }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            _SerializeState = serializeState;
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath
        {
            get { return _ObjectFilePath; }
        }

        /// <summary>
        /// 設定序列化繫結檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public void SetObjectFilePath(string filePath)
        {
            _ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// 物件建立時間。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// 服務端點位置，遠端連線為網址，近端連線為本地路徑。
        /// </summary>
        [Description("服務端點位置，遠端連線為網址，近端連線為本地路徑。")]
        [DefaultValue("")]
        public string Endpoint
        {
            get { return _Endpoint; }
            set { _Endpoint = value; }
        }

        /// <summary>
        /// 服務端點清單。
        /// </summary>
        [Description("服務端點清單。")]
        [DefaultValue(null)]
        public EndpointItemCollection EndpointItems
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _EndpointItems)) { return null; }
                if (_EndpointItems == null) { _EndpointItems = new EndpointItemCollection(); }
                return _EndpointItems;
            }
        }

    }
}
