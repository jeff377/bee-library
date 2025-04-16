using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 系統設定。
    /// </summary>
    [Serializable]
    [XmlType("SystemSettings")]
    [Description("系統設定。")]
    [TreeNode("系統設定")]
    public class TSystemSettings : IObjectSerializeFile, IObjectSerializeProcess
    {
        private TCommonConfiguration _CommonConfiguration = null;
        private TBackendConfiguration _BackendConfiguration = null;
        private TFrontendConfiguration _FrontendConfiguration = null;
        private TWebsiteConfiguration _WebsiteConfiguration = null;
        private TBackgroundServiceConfiguration _BackgroundServiceConfiguration = null;
        private TPropertyCollection _ExtendedProperties = null;

        #region 建構函式

        /// <summary>
        /// 建構函式 
        /// </summary>
        public TSystemSettings()
        {
            CreateTime = DateTime.Now;
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public ESerializeState SerializeState { get; private set; } = ESerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(ESerializeState serializeState)
        {
            SerializeState = serializeState;
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
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

        #region IObjectSerializeProcess 介面

        /// <summary>
        /// 執行序列化前的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void BeforeSerialize(ESerializeFormat serializeFormat)
        {
        }

        /// <summary>
        /// 執行序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void AfterSerialize(ESerializeFormat serializeFormat)
        {
        }

        /// <summary>
        /// 執行反序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void AfterDeserialize(ESerializeFormat serializeFormat)
        {
        }

        #endregion

        /// <summary>
        /// 物件建立時間。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; private set; }

        /// <summary>
        /// 通用參數及環境設置。
        /// </summary>
        [Description("通用參數及環境設置。")]
        [Browsable(false)]
        public TCommonConfiguration CommonConfiguration
        {
            get
            {
                if (_CommonConfiguration == null) { _CommonConfiguration = new TCommonConfiguration(); }
                return _CommonConfiguration;
            }
            set
            {
                _CommonConfiguration = value;
            }
        }

        /// <summary>
        /// 後端參數及環境設置。
        /// </summary>
        [Description("後端參數及環境設置。")]
        [Browsable(false)]
        public TBackendConfiguration BackendConfiguration
        {
            get
            {
                if (_BackendConfiguration == null) { _BackendConfiguration = new TBackendConfiguration(); }
                return _BackendConfiguration;
            }
            set
            {
                _BackendConfiguration = value;
            }
        }

        /// <summary>
        /// 前端參數及環境設置。
        /// </summary>
        [Description("前端參數及環境設置。")]
        [Browsable(false)]
        public TFrontendConfiguration FrontendConfiguration
        {
            get
            {
                if (_FrontendConfiguration == null) { _FrontendConfiguration = new TFrontendConfiguration(); }
                return _FrontendConfiguration;
            }
            set
            {
                _FrontendConfiguration = value;
            }
        }

        /// <summary>
        /// 網站參數及環境設置。
        /// </summary>
        [Description("網站參數及環境設置。")]
        [Browsable(false)]
        public TWebsiteConfiguration WebsiteConfiguration
        {
            get
            {
                if (_WebsiteConfiguration == null) { _WebsiteConfiguration = new TWebsiteConfiguration(); }
                return _WebsiteConfiguration;
            }
            set
            {
                _WebsiteConfiguration = value;
            }
        }

        /// <summary>
        /// 服務程式參數及環境設置。
        /// </summary>
        [Description("服務程式參數及環境設置。")]
        [Browsable(false)]
        public TBackgroundServiceConfiguration BackgroundServiceConfiguration
        {
            get
            {
                if (_BackgroundServiceConfiguration == null) { _BackgroundServiceConfiguration = new TBackgroundServiceConfiguration(); }
                return _BackgroundServiceConfiguration;
            }
            set
            {
                _BackgroundServiceConfiguration = value;
            }
        }

        /// <summary>
        /// 延伸屬性集合。
        /// </summary>
        [Description("延伸屬性集合。")]
        [DefaultValue(null)]
        public TPropertyCollection ExtendedProperties
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _ExtendedProperties)) { return null; }
                if (_ExtendedProperties == null) { _ExtendedProperties = new TPropertyCollection(); }
                return _ExtendedProperties;
            }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            // 系統主版琥
            SysInfo.Version = this.CommonConfiguration.Version;
            // 後端初始化
            this.BackendConfiguration.Initialize();
        }
    }
}
