using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// The framework type registry: one flat list mapping each progId to the types bound to it.
    /// </summary>
    /// <remarks>
    /// Modelled on the COM+ registry, which maps a ProgID to a component type and says nothing
    /// about where that program sits in a menu. Presentation is <see cref="MenuSettings"/>'s job;
    /// this definition holds only type bindings.
    /// <para>
    /// The list is flat rather than grouped by category because a progId is the registry key: a
    /// single <see cref="ProgramItemCollection"/> makes global uniqueness a property of the
    /// structure — a duplicate is rejected at load time — and turns lookup into one key hit. Under
    /// the earlier nested shape uniqueness held only within a category, so the same progId could
    /// appear twice and which one won depended on document order.
    /// </para>
    /// <para>
    /// Server-side only: it carries assembly-qualified type names that no client has any use for,
    /// so it is gated out of remote <c>GetDefine</c> alongside <see cref="SystemSettings"/> and
    /// <see cref="DatabaseSettings"/>.
    /// </para>
    /// </remarks>
    [Description("Program settings.")]
    [TreeNode("Program Settings")]
    public class ProgramSettings : IObjectSerializeFile
    {
        private ProgramItemCollection? _items = null;

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
        /// Sets the file path bound for serialization/deserialization.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// Gets the registered program collection, keyed by progId.
        /// </summary>
        [Description("Program item collection.")]
        [DefaultValue(null)]
        public ProgramItemCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _items!)) { return null; }
                if (_items == null) { _items = new ProgramItemCollection(this); }
                return _items;
            }
        }
    }
}
