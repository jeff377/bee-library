using System.Xml.Serialization;
using System.Text.Json.Serialization;
using Bee.Base;
using MessagePack;

namespace Bee.Definition.Organization
{
    /// <summary>
    /// A per-company department tree snapshot, keyed by company id. The serialisable state is the
    /// flat <see cref="Nodes"/> list (tri-modal: XML / JSON / MessagePack); the tree query index
    /// (parent→children, self-and-descendant sets) is built lazily and never serialised.
    /// </summary>
    /// <remarks>
    /// Loaded by <c>DepartmentTreeService</c> from the company database and cached per company so
    /// scope queries (<c>Dept</c> / <c>DeptAndSub</c>) run from memory. The flat node list is
    /// immutable after construction; the index is a read-only derivation (built once under a lock),
    /// so a cached instance is never mutated by queries.
    /// </remarks>
    [MessagePackObject]
    public class DepartmentTree : IKeyObject
    {
        /// <summary>
        /// Initializes a new empty <see cref="DepartmentTree"/> (required by serializers).
        /// </summary>
        public DepartmentTree() { }

        /// <summary>
        /// Initializes a new <see cref="DepartmentTree"/> from a flat node list.
        /// </summary>
        /// <param name="companyId">The company id (cache key).</param>
        /// <param name="nodes">The flat department nodes.</param>
        public DepartmentTree(string companyId, IEnumerable<DepartmentNode> nodes)
        {
            CompanyId = companyId ?? throw new ArgumentNullException(nameof(companyId));
            Nodes = [.. nodes ?? []];
        }

        /// <summary>Gets or sets the company id (cache key).</summary>
        [Key(100)]
        [XmlAttribute]
        public string CompanyId { get; set; } = string.Empty;

        /// <summary>Gets or sets the flat department nodes (the only serialised state).</summary>
        [Key(101)]
        [XmlArrayItem(typeof(DepartmentNode))]
        public DepartmentNodeCollection? Nodes { get; set; }

        /// <summary>
        /// Gets the item key value (the company id).
        /// </summary>
        public string GetKey() => CompanyId;

        // ---- lazy query index (not serialised) ----
        private Dictionary<Guid, DepartmentNode>? _byRowId;
        private Dictionary<Guid, List<Guid>>? _selfAndDescendants;
        private List<DepartmentNode>? _roots;
        private readonly object _indexLock = new();
        private volatile bool _indexBuilt;

        private void EnsureIndex()
        {
            if (_indexBuilt) { return; }
            lock (_indexLock)
            {
                if (_indexBuilt) { return; }

                var byRowId = BuildByRowId(Nodes);
                var (children, roots) = BuildChildrenAndRoots(byRowId);

                _byRowId = byRowId;
                _selfAndDescendants = BuildSelfAndDescendants(byRowId, children);
                _roots = roots;
                _indexBuilt = true;
            }
        }

        private static Dictionary<Guid, DepartmentNode> BuildByRowId(DepartmentNodeCollection? nodes)
        {
            var byRowId = new Dictionary<Guid, DepartmentNode>();
            foreach (var node in nodes ?? [])
            {
                byRowId[node.RowId] = node;
            }
            return byRowId;
        }

        private static (Dictionary<Guid, List<Guid>> children, List<DepartmentNode> roots) BuildChildrenAndRoots(
            Dictionary<Guid, DepartmentNode> byRowId)
        {
            var children = new Dictionary<Guid, List<Guid>>();
            var roots = new List<DepartmentNode>();
            foreach (var node in byRowId.Values)
            {
                // A node whose parent is empty or missing is treated as a root.
                if (node.ParentRowId == Guid.Empty || !byRowId.ContainsKey(node.ParentRowId))
                {
                    roots.Add(node);
                }
                else
                {
                    if (!children.TryGetValue(node.ParentRowId, out var list))
                    {
                        children[node.ParentRowId] = list = [];
                    }
                    list.Add(node.RowId);
                }
            }
            return (children, roots);
        }

        // Pre-compute the self-and-descendant set per node (iterative DFS with a visited guard
        // so a cyclic parent reference in dirty data cannot loop forever).
        private static Dictionary<Guid, List<Guid>> BuildSelfAndDescendants(
            Dictionary<Guid, DepartmentNode> byRowId,
            Dictionary<Guid, List<Guid>> children)
        {
            var selfAndDescendants = new Dictionary<Guid, List<Guid>>();
            foreach (var rowId in byRowId.Keys)
            {
                var result = new List<Guid>();
                var visited = new HashSet<Guid>();
                var stack = new Stack<Guid>();
                stack.Push(rowId);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (!visited.Add(current)) { continue; }
                    result.Add(current);
                    if (children.TryGetValue(current, out var kids))
                    {
                        foreach (var kid in kids) { stack.Push(kid); }
                    }
                }
                selfAndDescendants[rowId] = result;
            }
            return selfAndDescendants;
        }

        /// <summary>
        /// Returns the row id of the given department plus all of its descendants — the layer-2
        /// <c>DeptAndSub</c> set. Returns an empty list when the department is not in the tree.
        /// </summary>
        /// <param name="deptRowId">The department row id.</param>
        public IReadOnlyList<Guid> GetSelfAndDescendants(Guid deptRowId)
        {
            EnsureIndex();
            return _selfAndDescendants!.TryGetValue(deptRowId, out var set) ? set : [];
        }

        /// <summary>
        /// Returns the row id of the given department plus all of its ancestors up to the root.
        /// Returns an empty list when the department is not in the tree.
        /// </summary>
        /// <param name="deptRowId">The department row id.</param>
        public IReadOnlyList<Guid> GetSelfAndAncestors(Guid deptRowId)
        {
            EnsureIndex();
            if (!_byRowId!.ContainsKey(deptRowId)) { return []; }

            var result = new List<Guid>();
            var visited = new HashSet<Guid>();
            var current = deptRowId;
            while (visited.Add(current) && _byRowId.TryGetValue(current, out var node))
            {
                result.Add(current);
                if (node.ParentRowId == Guid.Empty) { break; }
                current = node.ParentRowId;
            }
            return result;
        }

        /// <summary>Returns whether the tree contains a department with the given row id.</summary>
        /// <param name="deptRowId">The department row id.</param>
        public bool Contains(Guid deptRowId)
        {
            EnsureIndex();
            return _byRowId!.ContainsKey(deptRowId);
        }

        /// <summary>Gets the department node by row id, or <c>null</c> when not in the tree.</summary>
        /// <param name="deptRowId">The department row id.</param>
        public DepartmentNode? GetNode(Guid deptRowId)
        {
            EnsureIndex();
            return _byRowId!.TryGetValue(deptRowId, out var node) ? node : null;
        }

        /// <summary>Gets the root department nodes (no parent, or parent outside the tree).</summary>
        [IgnoreMember]
        [XmlIgnore]
        [JsonIgnore]
        public IReadOnlyList<DepartmentNode> Roots
        {
            get
            {
                EnsureIndex();
                return _roots!;
            }
        }
    }
}
