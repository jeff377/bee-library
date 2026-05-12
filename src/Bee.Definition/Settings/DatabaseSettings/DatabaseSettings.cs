using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Database settings.
    /// </summary>
    [Description("Database settings.")]
    [TreeNode("Database Settings")]
    public class DatabaseSettings : IObjectSerializeFile, ISerializableClone
    {
        private DatabaseServerCollection? _servers = null;
        private DatabaseItemCollection? _items = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="DatabaseSettings"/>.
        /// </summary>
        public DatabaseSettings()
        {
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            _servers?.SetSerializeState(serializeState);
            _items?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Gets the file path bound to serialization.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
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

        #region ISerializableClone Interface

        /// <summary>
        /// Creates a serializable deep copy of this object.
        /// </summary>
        public object CreateSerializableCopy()
        {
            return Clone();
        }

        #endregion

        /// <summary>
        /// Gets the time at which this object was created.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Gets the database server collection.
        /// </summary>
        [Description("Database server collection.")]
        [DefaultValue(null)]
        public DatabaseServerCollection? Servers
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _servers!)) { return null; }
                if (_servers == null) { _servers = []; }
                return _servers;
            }
        }

        /// <summary>
        /// Gets the database connection settings collection.
        /// </summary>
        [Description("Database connection settings collection.")]
        [DefaultValue(null)]
        public DatabaseItemCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _items!)) { return null; }
                if (_items == null) { _items = []; }
                return _items;
            }
        }

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public DatabaseSettings Clone()
        {
            var copy = new DatabaseSettings();

            foreach (var server in Servers!)
                copy.Servers!.Add(server.Clone());

            foreach (var item in Items!)
                copy.Items!.Add(item.Clone());

            return copy;
        }


    }
}
