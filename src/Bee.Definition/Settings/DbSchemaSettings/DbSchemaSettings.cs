using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Serialization;
using Newtonsoft.Json;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Database schema settings.
    /// </summary>
    [Serializable]
    [XmlType("DbSchemaSettings")]
    [Description("Database schema settings.")]
    [TreeNode("Database Schema")]
    public class DbSchemaSettings : IObjectSerializeFile
    {
        private DbSchemaCollection _databases = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="DbSchemaSettings"/>.
        /// </summary>
        public DbSchemaSettings()
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
            BaseFunc.SetSerializeState(_databases, serializeState);
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
        /// Gets the database schema collection.
        /// </summary>
        [Description("Database schema collection.")]
        [DefaultValue(null)]
        public DbSchemaCollection Databases
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _databases)) { return null; }
                if (_databases == null) { _databases = new DbSchemaCollection(this); }
                return _databases;
            }
        }
    }
}
