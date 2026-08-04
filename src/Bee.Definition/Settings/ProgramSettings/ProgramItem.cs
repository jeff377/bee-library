using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A program item.
    /// </summary>
    [Description("Program item.")]
    [TreeNode]
    public class ProgramItem : KeyCollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramItem"/>.
        /// </summary>
        public ProgramItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramItem"/>.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public ProgramItem(string progId, string displayName)
        {
            this.ProgId = progId;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// Gets or sets the program ID.
        /// </summary>
        [XmlAttribute]
        [Description("Program ID.")]
        public string ProgId
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
        /// Gets or sets the assembly-qualified type name of the business object
        /// bound to this program. When empty, the framework falls back to the
        /// base form business object.
        /// </summary>
        /// <remarks>
        /// Expected format: <c>"Namespace.Type, AssemblyName"</c>
        /// (e.g. <c>"MyErp.Business.WorkOrderBo, MyErp.Business"</c>).
        /// Named after the role (the BO that handles this program) — matches the
        /// <c>BackendComponents</c> convention of using role names for type-name
        /// configuration properties.
        /// </remarks>
        [XmlAttribute]
        [Description("Business object bound to this program (assembly-qualified type name).")]
        [DefaultValue("")]
        public string BusinessObject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the assembly-qualified type name of the repository bound
        /// to this program. When empty, the framework falls back to the base data
        /// form repository.
        /// </summary>
        /// <remarks>
        /// Same format and naming rule as <see cref="BusinessObject"/>:
        /// <c>"Namespace.Type, AssemblyName"</c>.
        /// <para>
        /// The named type must derive from <c>DataFormRepository</c>, and unlike
        /// <see cref="BusinessObject"/> a name that will not load is <b>never</b> tolerated — the
        /// factory throws rather than degrading to the default. Data access has no harmless
        /// degraded mode: silently substituting the generic repository would run a custom program's
        /// reads and writes through logic its author replaced on purpose.
        /// </para>
        /// <para>
        /// Left empty for the reserved progIds. Their business objects are not schema-driven CRUD
        /// and reach data through the framework repositories instead.
        /// </para>
        /// </remarks>
        [XmlAttribute]
        [Description("Repository bound to this program (assembly-qualified type name).")]
        [DefaultValue("")]
        public string Repository { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.ProgId} - {this.DisplayName}";
        }
    }
}
