using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// One plugin bound to a program: the assembly-qualified name of a type deriving from the
    /// framework's form business plugin.
    /// </summary>
    /// <remarks>
    /// The type name is the item's key, so declaring the same plugin twice under one program is
    /// rejected when the definition loads rather than silently running it twice. One class binds to
    /// one stage, so a type never needs to appear twice under the same program.
    /// </remarks>
    [Description("Business plugin binding.")]
    [TreeNode]
    public class PluginItem : KeyCollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="PluginItem"/>.
        /// </summary>
        public PluginItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PluginItem"/>.
        /// </summary>
        /// <param name="type">The assembly-qualified type name of the plugin.</param>
        /// <param name="stage">The pipeline stage this plugin runs at.</param>
        public PluginItem(string type, PluginStage stage)
        {
            Type = type;
            Stage = stage;
        }

        #endregion

        /// <summary>
        /// Gets or sets the assembly-qualified type name of the plugin.
        /// </summary>
        /// <remarks>
        /// Expected format: <c>"Namespace.Type, AssemblyName"</c>
        /// (e.g. <c>"MyErp.Plugins.CreditLimitPlugin, MyErp.Plugins"</c>). The named type must
        /// derive from the framework's <c>FormBusinessPlugin</c> and override exactly the one stage
        /// this binding declares in <see cref="Stage"/>.
        /// </remarks>
        [XmlAttribute]
        [Description("Assembly-qualified type name of the plugin.")]
        public string Type
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the pipeline stage this plugin runs at.
        /// </summary>
        /// <remarks>
        /// A binding names exactly one stage, and the named type must override that stage and no
        /// other. Declaring the stage here is what lets the file be read for what runs where; the
        /// reflection that reads the class is kept as the check that the two agree, and a mismatch
        /// — in either direction — refuses to load rather than running something the file does not
        /// say. Omitting the attribute yields <see cref="PluginStage.None"/>, which is rejected.
        /// </remarks>
        [XmlAttribute]
        [DefaultValue(PluginStage.None)]
        [Description("Pipeline stage this plugin runs at.")]
        public PluginStage Stage { get; set; } = PluginStage.None;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.Type} ({this.Stage})";
        }
    }
}
