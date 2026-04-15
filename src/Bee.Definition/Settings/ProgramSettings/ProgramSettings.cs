using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
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
        private ProgramCategoryCollection _categories = null;

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
            BaseFunc.SetSerializeState(_categories, serializeState);
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
        /// <param name="fileName">The file name.</param>
        public void SetObjectFilePath(string fileName)
        {
            ObjectFilePath = fileName;
        }

        #endregion

        /// <summary>
        /// Gets the program category collection.
        /// </summary>
        [Description("Program category collection.")]
        [DefaultValue(null)]
        public ProgramCategoryCollection Categories
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _categories)) { return null; }
                if (_categories == null) { _categories = new ProgramCategoryCollection(this); }
                return _categories;
            }
        }
    }
}
