using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A leaf node that opens one registered program.
    /// </summary>
    /// <remarks>
    /// Named <see cref="MenuEntry"/> rather than <c>MenuItem</c> on purpose: this type is consumed by
    /// every UI head, and <c>MenuItem</c> is taken by practically all of them
    /// (<c>Avalonia.Controls.MenuItem</c>, <c>System.Windows.Controls.MenuItem</c>,
    /// WinForms' <c>ToolStripMenuItem</c>, third-party control suites). The collision would land
    /// precisely in the code that builds a menu from this definition — the one place that has both
    /// namespaces in scope.
    /// </remarks>
    [Description("Menu entry (leaf node opening one program).")]
    [TreeNode]
    public class MenuEntry : MenuNodeBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MenuEntry"/>.
        /// </summary>
        public MenuEntry()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MenuEntry"/>.
        /// </summary>
        /// <param name="id">The node ID.</param>
        /// <param name="progId">The program ID this entry opens.</param>
        /// <param name="caption">The caption.</param>
        public MenuEntry(string id, string progId, string caption)
        {
            Id = id;
            ProgId = progId;
            Caption = caption;
        }

        #endregion

        /// <summary>
        /// Gets or sets the program ID this entry opens — one entry in the
        /// <see cref="ProgramSettings"/> type registry.
        /// </summary>
        /// <remarks>
        /// Not the node key: several entries may open the same program (an order form reached both
        /// as "Orders" and as "Returns"), so program id to menu node is one-to-many. Track the
        /// currently open node by <see cref="MenuNodeBase.Id"/>, never by this value.
        /// </remarks>
        [XmlAttribute]
        [Description("Program ID this entry opens (references a ProgramSettings entry).")]
        [DefaultValue("")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.Id} - {this.Caption} ({this.ProgId})";
        }
    }
}
