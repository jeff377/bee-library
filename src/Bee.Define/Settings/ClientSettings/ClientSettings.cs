using Bee.Base;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

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
        private EndpointItemCollection _endpointItems = new EndpointItemCollection();

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
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// 設定序列化繫結檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
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
        public string Endpoint { get; set; } = string.Empty;

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
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _endpointItems)) { return null; }
                if (_endpointItems == null) { _endpointItems = new EndpointItemCollection(); }
                return _endpointItems;
            }
        }

    }
}
