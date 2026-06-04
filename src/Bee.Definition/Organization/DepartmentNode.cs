using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Organization
{
    /// <summary>
    /// A single department node in a company's department tree. A flat, serialisable record
    /// (XML attribute / JSON / MessagePack); the tree structure is derived from
    /// <see cref="ParentRowId"/> by <see cref="DepartmentTree"/>.
    /// </summary>
    [MessagePackObject]
    public sealed class DepartmentNode : MessagePackCollectionItem
    {
        /// <summary>
        /// Initializes a new empty <see cref="DepartmentNode"/> (required by serializers).
        /// </summary>
        public DepartmentNode() { }

        /// <summary>
        /// Initializes a new <see cref="DepartmentNode"/>.
        /// </summary>
        /// <param name="rowId">The department row id (<c>ft_department.sys_rowid</c>).</param>
        /// <param name="deptId">The department business id (<c>sys_id</c>).</param>
        /// <param name="deptName">The department name (<c>sys_name</c>).</param>
        /// <param name="parentRowId">The parent department row id; <see cref="System.Guid.Empty"/> for a root.</param>
        /// <param name="managerRowId">The manager (employee) row id.</param>
        public DepartmentNode(Guid rowId, string deptId, string deptName, Guid parentRowId, Guid managerRowId)
        {
            RowId = rowId;
            DeptId = deptId;
            DeptName = deptName;
            ParentRowId = parentRowId;
            ManagerRowId = managerRowId;
        }

        /// <summary>Gets or sets the department row id (<c>ft_department.sys_rowid</c>).</summary>
        [Key(100)]
        [XmlAttribute]
        public Guid RowId { get; set; }

        /// <summary>Gets or sets the department business id (<c>sys_id</c>).</summary>
        [Key(101)]
        [XmlAttribute]
        public string DeptId { get; set; } = string.Empty;

        /// <summary>Gets or sets the department name (<c>sys_name</c>).</summary>
        [Key(102)]
        [XmlAttribute]
        public string DeptName { get; set; } = string.Empty;

        /// <summary>Gets or sets the parent department row id; <see cref="System.Guid.Empty"/> for a root node.</summary>
        [Key(103)]
        [XmlAttribute]
        public Guid ParentRowId { get; set; }

        /// <summary>Gets or sets the manager (employee) row id.</summary>
        [Key(104)]
        [XmlAttribute]
        public Guid ManagerRowId { get; set; }
    }
}
