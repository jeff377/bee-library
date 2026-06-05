using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of per-action permission rules, keyed by action.
    /// </summary>
    [Description("Permission rule collection.")]
    [TreeNode("Rules", false)]
    public class PermissionRuleCollection : KeyCollectionBase<PermissionRule>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PermissionRuleCollection"/>.
        /// </summary>
        /// <param name="model">The owning permission model.</param>
        public PermissionRuleCollection(PermissionModel model) : base(model)
        { }

        /// <summary>
        /// Adds a permission rule to the collection.
        /// </summary>
        /// <param name="action">The permission action.</param>
        /// <param name="scope">The record-scope strategy.</param>
        public PermissionRule Add(PermissionAction action, ScopeStrategy scope = ScopeStrategy.Inherit)
        {
            var rule = new PermissionRule(action, scope);
            base.Add(rule);
            return rule;
        }
    }
}
