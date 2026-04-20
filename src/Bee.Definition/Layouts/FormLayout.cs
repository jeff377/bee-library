using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Form layout configuration.
    /// </summary>
    [Serializable]
    [XmlType("FormLayout")]
    [Description("Form layout configuration.")]
    [TreeNode]
    public class FormLayout : IObjectSerializeFile
    {
        private LayoutGroupCollection? _groups = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="FormLayout"/>.
        /// </summary>
        public FormLayout()
        {
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
            BaseFunc.SetSerializeState(_groups!, serializeState);
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
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the form layout ID.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Form layout ID.")]
        public string LayoutId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the layout group collection.
        /// </summary>
        [Description("Layout group collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public LayoutGroupCollection? Groups
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _groups!)) { return null; }
                if (_groups == null) { _groups = []; }
                return _groups;
            }
        }

        /// <summary>
        /// Finds the layout item for the specified field name.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        public LayoutItem? FindItem(string fieldName)
        {
            foreach (LayoutGroup group in this.Groups!)
            {
                foreach (LayoutItemBase baseItem in group.Items!)
                {
                    if (baseItem is LayoutItem item && StrFunc.IsEquals(item.FieldName, fieldName))
                        return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.LayoutId} - {this.DisplayName}";
        }
    }
}
