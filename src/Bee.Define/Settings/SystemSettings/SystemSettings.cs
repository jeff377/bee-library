using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// System settings.
    /// </summary>
    [Serializable]
    [XmlType("SystemSettings")]
    [Description("System settings.")]
    [TreeNode("System Settings")]
    public class SystemSettings : IObjectSerializeFile
    {
        private PropertyCollection _ExtendedProperties = null;

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public SystemSettings()
        {
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Set serialization state.
        /// </summary>
        /// <param name="serializeState">Serialization state.</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
        }

        /// <summary>
        /// Serialized binding file.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Set serialized binding file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// Object creation time.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Common parameters and environment settings.
        /// </summary>
        [Description("Common parameters and environment settings.")]
        [Browsable(false)]
        public CommonConfiguration CommonConfiguration { get; set; } = new CommonConfiguration();

        /// <summary>
        /// Backend parameters and environment settings.
        /// </summary>
        [Description("Backend parameters and environment settings.")]
        [Browsable(false)]
        public BackendConfiguration BackendConfiguration { get; set; } = new BackendConfiguration();

        /// <summary>
        /// Frontend parameters and environment settings.
        /// </summary>
        [Description("Frontend parameters and environment settings.")]
        [Browsable(false)]
        public FrontendConfiguration FrontendConfiguration { get; set; } = new FrontendConfiguration();

        /// <summary>
        /// Website parameters and environment settings.
        /// </summary>
        [Description("Website parameters and environment settings.")]
        [Browsable(false)]
        public WebsiteConfiguration WebsiteConfiguration { get; set; } = new WebsiteConfiguration();

        /// <summary>
        /// Background service parameters and environment settings.
        /// </summary>
        [Description("Background service parameters and environment settings.")]
        [Browsable(false)]
        public BackgroundServiceConfiguration BackgroundServiceConfiguration { get; set; } = new BackgroundServiceConfiguration();

        /// <summary>
        /// Extended property collection.
        /// </summary>
        [Description("Extended property collection.")]
        [DefaultValue(null)]
        public PropertyCollection ExtendedProperties
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _ExtendedProperties)) { return null; }
                if (_ExtendedProperties == null) { _ExtendedProperties = new PropertyCollection(); }
                return _ExtendedProperties;
            }
        }

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
