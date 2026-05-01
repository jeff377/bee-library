using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Program settings (program list).
    /// </summary>
    [Serializable]
    [XmlType("ProgramSettings")]
    [Description("Program settings.")]
    [TreeNode("Program Settings")]
    public class ProgramSettings : IObjectSerializeFile
    {
        private ProgramCategoryCollection? _categories = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramSettings"/>.
        /// </summary>
        public ProgramSettings()
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
        /// Sets the file path bound for serialization/deserialization.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// Gets the program category collection.
        /// </summary>
        [Description("Program category collection.")]
        [DefaultValue(null)]
        public ProgramCategoryCollection? Categories
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _categories!)) { return null; }
                if (_categories == null) { _categories = new ProgramCategoryCollection(this); }
                return _categories;
            }
        }
    }
}
