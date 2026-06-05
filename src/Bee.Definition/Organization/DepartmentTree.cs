using System.Xml.Serialization;
using Bee.Base;
using MessagePack;

namespace Bee.Definition.Organization
{
    /// <summary>
    /// A per-company department tree snapshot, keyed by company id. The serialisable state is the
    /// nested <see cref="Roots"/> forest — each <see cref="DepartmentNode"/> carries its children —
    /// so XML / JSON / MessagePack all round-trip the hierarchy directly (a front end renders it as
    /// a tree without re-assembly). The query index (row-id lookup, parent map) is built lazily and
    /// never serialised.
    /// </summary>
    /// <remarks>
    /// Loaded by <c>DepartmentTreeService</c> from the company database (flat <see cref="DepartmentRow"/>
    /// rows) and cached per company so scope queries (<c>Dept</c> / <c>DeptAndSub</c>) run from memory.
    /// The nested forest is immutable after construction; the index is a read-only derivation (built
    /// once under a lock), so a cached instance is never mutated by queries.
    /// </remarks>
    [MessagePackObject]
    public class DepartmentTree : IKeyObject
    {
        /// <summary>
        /// Initializes a new empty <see cref="DepartmentTree"/> (required by serializers).
        /// </summary>
        public DepartmentTree() { }

        /// <summary>
        /// Initializes a new <see cref="DepartmentTree"/> by assembling flat department rows into a
        /// nested forest. Rows whose parent is empty or absent become roots; a cyclic parent chain in
        /// dirty data is broken (the closing edge is dropped) so the forest stays acyclic.
        /// </summary>
        /// <param name="companyId">The company id (cache key).</param>
        /// <param name="rows">The flat department rows.</param>
        public DepartmentTree(string companyId, IEnumerable<DepartmentRow> rows)
        {
            CompanyId = companyId ?? throw new ArgumentNullException(nameof(companyId));
            Roots = BuildForest(rows ?? []);
        }

        /// <summary>Gets or sets the company id (cache key).</summary>
        [Key(100)]
        [XmlAttribute]
        public string CompanyId { get; set; } = string.Empty;

        /// <summary>Gets or sets the root department nodes (each nests its children); the serialised state.</summary>
        [Key(101)]
        [XmlArrayItem(typeof(DepartmentNode))]
        public DepartmentNodeCollection? Roots { get; set; }

        /// <summary>
        /// Gets the item key value (the company id).
        /// </summary>
        public string GetKey() => CompanyId;

        // ---- forest assembly (flat rows -> nested nodes) ----

        private static DepartmentNodeCollection BuildForest(IEnumerable<DepartmentRow> rows)
        {
            var rowList = rows as IReadOnlyList<DepartmentRow> ?? [.. rows];
            var nodes = new Dictionary<Guid, DepartmentNode>(rowList.Count);
            var parentOf = new Dictionary<Guid, Guid>(rowList.Count);
            foreach (var row in rowList)
            {
                nodes[row.RowId] = new DepartmentNode(row.RowId, row.DeptId, row.DeptName, row.ManagerRowId);
                parentOf[row.RowId] = row.ParentRowId;
            }

            var forest = new DepartmentNodeCollection();
            foreach (var rowId in rowList.Select(row => row.RowId))
            {
                var parentRowId = parentOf[rowId];
                if (parentRowId != Guid.Empty
                    && nodes.TryGetValue(parentRowId, out var parent)
                    && IsSafeEdge(rowId, parentRowId, parentOf))
                {
                    (parent.Children ??= []).Add(nodes[rowId]);
                }
                else
                {
                    forest.Add(nodes[rowId]);
                }
            }
            return forest;
        }

        // Returns false when attaching child under parent would close a cycle — i.e. walking up from
        // parent (via the flat parent map) reaches child again. Dropping such an edge makes the child
        // a root, so a cyclic input degrades to a flat forest instead of an infinite object graph.
        private static bool IsSafeEdge(Guid child, Guid parent, Dictionary<Guid, Guid> parentOf)
        {
            var visited = new HashSet<Guid> { child };
            var current = parent;
            while (current != Guid.Empty && parentOf.TryGetValue(current, out var next))
            {
                if (current == child) { return false; }
                if (!visited.Add(current)) { return false; }
                current = next;
            }
            return true;
        }

        // ---- lazy query index (not serialised) ----
        private Dictionary<Guid, DepartmentNode>? _byRowId;
        private Dictionary<Guid, Guid>? _parentOf;
        private readonly object _indexLock = new();
        private volatile bool _indexBuilt;

        private void EnsureIndex()
        {
            if (_indexBuilt) { return; }
            lock (_indexLock)
            {
                if (_indexBuilt) { return; }

                var byRowId = new Dictionary<Guid, DepartmentNode>();
                var parentOf = new Dictionary<Guid, Guid>();
                var stack = new Stack<(DepartmentNode node, Guid parentRowId)>();
                foreach (var root in Roots ?? []) { stack.Push((root, Guid.Empty)); }
                while (stack.Count > 0)
                {
                    var (node, parentRowId) = stack.Pop();
                    // TryAdd guards against a shared/duplicate node reference looping the walk.
                    if (!byRowId.TryAdd(node.RowId, node)) { continue; }
                    parentOf[node.RowId] = parentRowId;
                    foreach (var child in node.Children ?? []) { stack.Push((child, node.RowId)); }
                }

                _byRowId = byRowId;
                _parentOf = parentOf;
                _indexBuilt = true;
            }
        }

        /// <summary>
        /// Returns the row id of the given department plus all of its descendants — the layer-2
        /// <c>DeptAndSub</c> set. Returns an empty list when the department is not in the tree.
        /// </summary>
        /// <param name="deptRowId">The department row id.</param>
        public IReadOnlyList<Guid> GetSelfAndDescendants(Guid deptRowId)
        {
            EnsureIndex();
            if (!_byRowId!.TryGetValue(deptRowId, out var start)) { return []; }

            var result = new List<Guid>();
            var visited = new HashSet<Guid>();
            var stack = new Stack<DepartmentNode>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (!visited.Add(node.RowId)) { continue; }
                result.Add(node.RowId);
                foreach (var child in node.Children ?? []) { stack.Push(child); }
            }
            return result;
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
            while (visited.Add(current) && _byRowId.ContainsKey(current))
            {
                result.Add(current);
                if (!_parentOf!.TryGetValue(current, out var parent) || parent == Guid.Empty) { break; }
                current = parent;
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
    }
}
