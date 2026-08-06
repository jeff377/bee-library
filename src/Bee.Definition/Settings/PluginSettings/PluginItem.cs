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
    /// rejected when the definition loads rather than silently running it twice.
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
        public PluginItem(string type)
        {
            Type = type;
        }

        #endregion

        /// <summary>
        /// Gets or sets the assembly-qualified type name of the plugin.
        /// </summary>
        /// <remarks>
        /// Expected format: <c>"Namespace.Type, AssemblyName"</c>
        /// (e.g. <c>"MyErp.Plugins.CreditLimitPlugin, MyErp.Plugins"</c>). The named type must
        /// derive from the framework's <c>FormBusinessPlugin</c> and override at least one of its
        /// stages — a plugin that overrides none is bound but can never run, which the maintenance
        /// API rejects when the definition is saved.
        /// </remarks>
        [XmlAttribute]
        [Description("Assembly-qualified type name of the plugin.")]
        public string Type
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return this.Type;
        }
    }
}
