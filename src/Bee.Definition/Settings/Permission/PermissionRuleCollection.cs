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
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public PermissionRuleCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionRuleCollection"/>.
        /// </summary>
        /// <param name="model">The owning permission model.</param>
        public PermissionRuleCollection(PermissionModel model) : base(model)
        { }
    }

    /// <summary>
    /// Extension methods for <see cref="PermissionRuleCollection"/>.
    /// </summary>
    public static class PermissionRuleCollectionExtensions
    {
        /// <summary>
        /// Adds a permission rule to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="action">The permission action.</param>
        /// <param name="scope">The record-scope strategy.</param>
        public static PermissionRule Add(this PermissionRuleCollection? collection, PermissionAction action, ScopeStrategy scope = ScopeStrategy.Inherit)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var rule = new PermissionRule(action, scope);
            collection.Add(rule);
            return rule;
        }
    }
}
