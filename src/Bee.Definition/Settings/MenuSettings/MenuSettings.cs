using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// The application menu: an ordered, arbitrarily deep tree of <see cref="MenuFolder"/> and
    /// <see cref="MenuEntry"/> nodes. Purely presentational — grouping, ordering, captions,
    /// icons and the design-time visibility switch.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="ProgramSettings"/>, which is the type registry
    /// (progId to business object / repository) and carries nothing presentational. The two are
    /// separate definitions because their readers differ: only the server reads the registry, only
    /// a client reads the menu. An entry references the registry through
    /// <see cref="MenuEntry.ProgId"/>.
    /// </remarks>
    [Description("Menu settings.")]
    [TreeNode("Menu Settings")]
    public class MenuSettings : IObjectSerializeFile
    {
        private MenuNodeCollection? _items = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MenuSettings"/>.
        /// </summary>
        public MenuSettings()
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
        /// Gets the root node collection.
        /// </summary>
        /// <remarks>
        /// Each subtype is declared with its own <see cref="XmlArrayItemAttribute"/> so the
        /// serializer writes <c>&lt;MenuFolder&gt;</c> and <c>&lt;MenuEntry&gt;</c> elements rather
        /// than one element name plus an <c>xsi:type</c> discriminator.
        /// </remarks>
        [Description("Root node collection.")]
        [DefaultValue(null)]
        [XmlArrayItem(typeof(MenuFolder))]
        [XmlArrayItem(typeof(MenuEntry))]
        public MenuNodeCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _items!)) { return null; }
                if (_items == null) { _items = new MenuNodeCollection(this); }
                return _items;
            }
        }

        /// <summary>
        /// Walks the whole tree depth-first, in document order.
        /// </summary>
        public IEnumerable<MenuNodeBase> EnumerateNodes()
            => EnumerateNodes(Items);

        private static IEnumerable<MenuNodeBase> EnumerateNodes(MenuNodeCollection? nodes)
        {
            if (nodes == null) { yield break; }
            foreach (var node in nodes)
            {
                yield return node;
                if (node is MenuFolder folder)
                {
                    foreach (var child in EnumerateNodes(folder.Items))
                        yield return child;
                }
            }
        }

        /// <summary>
        /// Finds a node anywhere in the tree by its <see cref="MenuNodeBase.Id"/>.
        /// </summary>
        /// <param name="id">The node ID.</param>
        /// <returns>The node, or <c>null</c> when no node carries that ID.</returns>
        public MenuNodeBase? FindNode(string id)
            => EnumerateNodes().FirstOrDefault(node => StringUtilities.IsEquals(node.Id, id));

        /// <summary>
        /// Returns every problem found in the menu tree; an empty list means the definition is valid.
        /// </summary>
        /// <param name="registry">
        /// The type registry to check <see cref="MenuEntry.ProgId"/> references against, or
        /// <c>null</c> to skip the referential check (the structural checks always run).
        /// </param>
        /// <remarks>
        /// <see cref="MenuNodeCollection"/> guarantees key uniqueness among siblings only, so the
        /// cross-tree uniqueness that makes <see cref="MenuNodeBase.Id"/> a stable reference has to
        /// be checked by walking the tree.
        /// <para>
        /// An empty folder is not reported: it is a reasonable intermediate state while authoring.
        /// </para>
        /// </remarks>
        public IReadOnlyList<string> Validate(ProgramSettings? registry = null)
        {
            var problems = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in EnumerateNodes())
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    problems.Add($"A {node.GetType().Name} node has an empty Id.");
                }
                else if (!seenIds.Add(node.Id))
                {
                    problems.Add($"Menu node Id '{node.Id}' is used more than once; ids must be unique across the whole tree.");
                }

                if (node is not MenuEntry entry) { continue; }

                if (string.IsNullOrWhiteSpace(entry.ProgId))
                {
                    problems.Add($"MenuEntry '{entry.Id}' has an empty ProgId.");
                }
                else if (registry?.Items?.Contains(entry.ProgId) == false)
                {
                    problems.Add($"MenuEntry '{entry.Id}' references ProgId '{entry.ProgId}', which is not registered in ProgramSettings.");
                }
            }

            return problems;
        }

        /// <summary>
        /// Throws when the menu tree is invalid; otherwise returns silently.
        /// </summary>
        /// <param name="registry">
        /// The type registry to check <see cref="MenuEntry.ProgId"/> references against, or
        /// <c>null</c> to run the structural checks only.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="Validate"/> reports any problem.</exception>
        /// <remarks>
        /// Each storage calls this right after deserialization, so a malformed menu surfaces where
        /// it is read rather than as a puzzling absence later on. Every problem is listed in one
        /// message: fixing them one round-trip at a time would be needless work for the maintainer.
        /// </remarks>
        public void EnsureValid(ProgramSettings? registry = null)
        {
            var problems = Validate(registry);
            if (problems.Count == 0) { return; }

            throw new InvalidOperationException(
                "MenuSettings is invalid:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
        }
    }
}
