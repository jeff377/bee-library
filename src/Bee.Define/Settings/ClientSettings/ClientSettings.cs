using Bee.Base;
using Bee.Base.Serialization;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define.Settings
{
    /// <summary>
    /// Client settings.
    /// </summary>
    [Serializable]
    [XmlType("ClientSettings")]
    [Description("Client settings.")]
    public class ClientSettings : IObjectSerializeFile
    {
        private EndpointItemCollection _endpointItems = new EndpointItemCollection();

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ClientSettings"/>.
        /// </summary>
        public ClientSettings()
        {
            CreateTime = DateTime.Now;
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
        }

        /// <summary>
        /// Gets the file path bound to serialization.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Sets the file path bound to serialization.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// Gets the time at which this object was created.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the service endpoint location. Use a URL for remote connections or a local path for local connections.
        /// </summary>
        [Description("Service endpoint location. Use a URL for remote connections or a local path for local connections.")]
        [DefaultValue("")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets the service endpoint list.
        /// </summary>
        [Description("Service endpoint list.")]
        [DefaultValue(null)]
        public EndpointItemCollection EndpointItems
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _endpointItems)) { return null; }
                if (_endpointItems == null) { _endpointItems = new EndpointItemCollection(); }
                return _endpointItems;
            }
        }

    }
}
