using Bee.Base.Serialization;
using System.Text.Json.Serialization;
using MessagePack;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Client settings.
    /// </summary>
    [Description("Client settings.")]
    public class ClientSettings : IObjectSerializeFile
    {
        private EndpointItemCollection _endpointItems = [];

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ClientSettings"/>.
        /// </summary>
        public ClientSettings()
        {
            CreateTime = DateTime.UtcNow;
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
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
        [XmlIgnore, JsonIgnore, IgnoreMember]
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
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public DateTime CreateTime { get; private set; }

        /// <summary>
        /// Gets or sets the service endpoint location. Use a URL for remote connections or a local path for local connections.
        /// </summary>
        [Description("Service endpoint location. Use a URL for remote connections or a local path for local connections.")]
        [DefaultValue("")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key sent as the <c>X-Api-Key</c> header, identifying which
        /// application is calling.
        /// </summary>
        /// <remarks>
        /// NOTE: not a secret in the cryptographic sense — a key shipped inside a client can always
        /// be recovered from it. Keeping it here rather than in source is about being able to change
        /// it without recompiling; user authentication stays with the access token.
        /// </remarks>
        [Description("API key sent as the X-Api-Key header, identifying the calling application.")]
        [DefaultValue("")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets the service endpoint list.
        /// </summary>
        [Description("Service endpoint list.")]
        [DefaultValue(null)]
        public EndpointItemCollection? EndpointItems
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _endpointItems)) { return null; }
                if (_endpointItems == null) { _endpointItems = []; }
                return _endpointItems;
            }
        }

    }
}
