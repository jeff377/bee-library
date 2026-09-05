using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Business plugins: per progId, an ordered chain of types that run at fixed points of the
    /// save and delete pipelines.
    /// </summary>
    /// <remarks>
    /// The lightweight counterpart of <see cref="ProgramSettings"/>. The registry answers "which
    /// business object <b>is</b> this program"; this definition answers "what else runs when it
    /// saves or deletes" — without replacing the program's business object at all.
    /// <para>
    /// The two are separate definitions rather than one because their shapes disagree. A registry
    /// entry binds one type per role and a customization overrides it property by property; a
    /// plugin chain is ordered and many-per-program, and the layers add up instead of overriding.
    /// Keeping both in one file would put two overlay granularities in one place. The registry file
    /// is also rewritten wholesale when the host self-registers a missing reserved progId, which is
    /// no place for a hand-authored ordered list.
    /// </para>
    /// <para>
    /// Server-side only: plugins execute inside business objects, so no client reads this.
    /// </para>
    /// </remarks>
    [Description("Plugin settings.")]
    [TreeNode("Plugin Settings")]
    public class PluginSettings : IObjectSerializeFile
    {
        private ProgramPluginItemCollection? _items = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="PluginSettings"/>.
        /// </summary>
        public PluginSettings()
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
        /// Gets the per-program plugin chains, keyed by progId.
        /// </summary>
        [Description("Program plugin item collection.")]
        [DefaultValue(null)]
        public ProgramPluginItemCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _items!)) { return null; }
                if (_items == null) { _items = new ProgramPluginItemCollection(this); }
                return _items;
            }
        }

        /// <summary>
        /// Returns the plugin bindings of a progId, in declaration order; an empty array when the
        /// program declares none.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <remarks>
        /// Value copies rather than the <see cref="PluginItem"/> instances: this settings object is
        /// held in a process-wide cache, and handing out its items would let a caller mutate what
        /// every session reads.
        /// </remarks>
        public IReadOnlyList<PluginBinding> GetPluginBindings(string progId)
        {
            var item = Items?.GetOrDefault(progId);
            if (item?.Plugins == null || item.Plugins.Count == 0) { return []; }
            return item.Plugins.Select(plugin => new PluginBinding(plugin.Type, plugin.Stage)).ToArray();
        }
    }
}
