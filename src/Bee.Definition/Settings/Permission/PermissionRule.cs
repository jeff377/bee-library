using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A single per-action permission rule: the action plus its record-scope strategy.
    /// Keyed by <see cref="Action"/> within a model (each action appears at most once).
    /// </summary>
    [Description("Permission rule.")]
    [TreeNode]
    public class PermissionRule : KeyCollectionItem
    {
        private PermissionActions _action = PermissionActions.None;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionRule"/>.
        /// </summary>
        public PermissionRule()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionRule"/>.
        /// </summary>
        /// <param name="action">The permission action.</param>
        /// <param name="scope">The record-scope strategy.</param>
        public PermissionRule(PermissionActions action, ScopeStrategy scope = ScopeStrategy.Inherit)
        {
            Action = action;
            Scope = scope;
        }

        #endregion

        /// <summary>
        /// Gets or sets the permission action. Also serves as the collection key.
        /// </summary>
        [XmlAttribute]
        [Description("Permission action.")]
        public PermissionActions Action
        {
            get { return _action; }
            set
            {
                _action = value;
                base.Key = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the record-scope strategy. Defaults to <see cref="ScopeStrategy.Inherit"/>;
        /// egress actions (Print / Export) omit the scope and inherit the model's Read scope.
        /// </summary>
        [XmlAttribute]
        [Description("Record-scope strategy.")]
        [DefaultValue(ScopeStrategy.Inherit)]
        public ScopeStrategy Scope { get; set; } = ScopeStrategy.Inherit;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.Action} : {this.Scope}";
        }
    }
}
