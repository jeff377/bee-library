using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Organization
{
    /// <summary>
    /// A single node in a company's department tree, holding its own <see cref="Children"/> so the
    /// hierarchy is expressed by nesting (tri-modal: XML / JSON / MessagePack). The flat database
    /// rows are assembled into this nested shape by <see cref="DepartmentTree"/>; the load-time
    /// parent pointer lives on <see cref="DepartmentRow"/>, not here.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class DepartmentNode : MessagePackCollectionItem
    {
        /// <summary>
        /// Initializes a new empty <see cref="DepartmentNode"/> (required by serializers).
        /// </summary>
        public DepartmentNode() { }

        /// <summary>
        /// Initializes a new <see cref="DepartmentNode"/>.
        /// </summary>
        /// <param name="rowId">The department row id (<c>st_department.sys_rowid</c>).</param>
        /// <param name="deptId">The department business id (<c>sys_id</c>).</param>
        /// <param name="deptName">The department name (<c>sys_name</c>).</param>
        /// <param name="managerRowId">The manager (employee) row id.</param>
        public DepartmentNode(Guid rowId, string deptId, string deptName, Guid managerRowId)
        {
            RowId = rowId;
            DeptId = deptId;
            DeptName = deptName;
            ManagerRowId = managerRowId;
        }

        /// <summary>Gets or sets the department row id (<c>st_department.sys_rowid</c>).</summary>
        [XmlAttribute]
        public Guid RowId { get; set; }

        /// <summary>Gets or sets the department business id (<c>sys_id</c>).</summary>
        [XmlAttribute]
        public string DeptId { get; set; } = string.Empty;

        /// <summary>Gets or sets the department name (<c>sys_name</c>).</summary>
        [XmlAttribute]
        public string DeptName { get; set; } = string.Empty;

        /// <summary>Gets or sets the manager (employee) row id.</summary>
        [XmlAttribute]
        public Guid ManagerRowId { get; set; }

        /// <summary>Gets or sets the child department nodes; <c>null</c> for a leaf.</summary>
        [XmlArrayItem(typeof(DepartmentNode))]
        public DepartmentNodeCollection? Children { get; set; }

        /// <summary>
        /// Determines whether the <see cref="Children"/> property should be serialized — XmlSerializer
        /// honours <c>ShouldSerialize{PropertyName}()</c>, so a leaf omits an empty <c>Children</c> element.
        /// </summary>
        public bool ShouldSerializeChildren() => Children != null && Children.Count > 0;
    }
}
