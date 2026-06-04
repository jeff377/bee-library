using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Collections;
using Bee.Base.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A permission target model (an aggregate-root business entity), declaring the actions
    /// it supports and the default record-scope strategy per action. The model id is a
    /// PascalCase business entity name, deliberately distinct from a form's progId.
    /// </summary>
    [Description("Permission model.")]
    [TreeNode]
    public class PermissionModel : KeyCollectionItem
    {
        private PermissionRuleCollection? _rules = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionModel"/>.
        /// </summary>
        public PermissionModel()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionModel"/>.
        /// </summary>
        /// <param name="modelId">The model id (PascalCase business entity).</param>
        /// <param name="displayName">The display name.</param>
        public PermissionModel(string modelId, string displayName)
        {
            ModelId = modelId;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// Gets or sets the model id (PascalCase business entity, e.g. <c>"PurchaseOrder"</c>).
        /// </summary>
        [XmlAttribute]
        [Description("Model id.")]
        public string ModelId
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the per-action permission rule collection.
        /// </summary>
        [Description("Permission rule collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public PermissionRuleCollection? Rules
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _rules!)) { return null; }
                if (_rules == null) { _rules = new PermissionRuleCollection(this); }
                return _rules;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            _rules?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.ModelId} - {this.DisplayName}";
        }
    }
}
