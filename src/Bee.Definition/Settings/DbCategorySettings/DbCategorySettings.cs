using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Database category settings.
    /// </summary>
    [Description("Database category settings.")]
    [TreeNode("Database Categories")]
    public class DbCategorySettings : IObjectSerializeFile
    {
        private DbCategoryCollection? _categories = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="DbCategorySettings"/>.
        /// </summary>
        public DbCategorySettings()
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
            _categories?.SetSerializeState(serializeState);
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

        /// <summary>
        /// Gets the time at which this object was created.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Gets the database category collection.
        /// </summary>
        [Description("Database category collection.")]
        [DefaultValue(null)]
        public DbCategoryCollection? Categories
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _categories!)) { return null; }
                if (_categories == null) { _categories = new DbCategoryCollection(this); }
                return _categories;
            }
        }
    }
}
