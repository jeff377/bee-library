using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A declarative business rule evaluated by the rule engine at a save/delete lifecycle
    /// point (see <see cref="Trigger"/>). Lets a form express field validation and lifecycle
    /// guards through expressions instead of hand-written business-object code.
    /// </summary>
    /// <remarks>
    /// Evaluation is two-stage: <see cref="When"/> decides whether the rule applies (empty =
    /// always; <c>false</c> = the rule is skipped), and only then is <see cref="Condition"/>
    /// checked. A <see cref="Condition"/> evaluating to <c>false</c> is a violation: the engine
    /// aborts the operation and surfaces <see cref="Message"/> to the user.
    /// </remarks>
    [Description("Business rule.")]
    [TreeNode]
    public class FormRule : KeyCollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="FormRule"/>.
        /// </summary>
        public FormRule()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="FormRule"/>.
        /// </summary>
        /// <param name="ruleId">The rule id.</param>
        /// <param name="condition">The condition that must hold for the rule to pass.</param>
        /// <param name="message">The message shown when the condition fails.</param>
        public FormRule(string ruleId, string condition, string message)
        {
            RuleId = ruleId;
            Condition = condition;
            Message = message;
        }

        #endregion

        /// <summary>
        /// Gets or sets the rule id (unique within the owning schema).
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Rule id.")]
        public string RuleId
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the lifecycle point at which this rule is evaluated.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Lifecycle point at which the rule is evaluated.")]
        [DefaultValue(FormRuleTrigger.BeforeSave)]
        public FormRuleTrigger Trigger { get; set; } = FormRuleTrigger.BeforeSave;

        /// <summary>
        /// Gets or sets the name of the table this rule applies to. Empty targets the master
        /// table; a detail table name evaluates the rule per detail row.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Target table name; empty targets the master table.")]
        [DefaultValue("")]
        public string TargetTable { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the applicability expression that decides whether this rule is checked.
        /// Must evaluate to a boolean. Empty means the rule always applies; a <c>false</c> result
        /// skips the rule (treated as passing) without evaluating <see cref="Condition"/>.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Applicability expression; empty always applies, false skips the rule.")]
        [DefaultValue("")]
        public string When { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the validation expression that must hold for the rule to pass. Must
        /// evaluate to a boolean; a <c>false</c> result is a violation that aborts the operation
        /// and surfaces <see cref="Message"/>.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Validation expression; a false result is a violation.")]
        [DefaultValue("")]
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message shown to the user when <see cref="Condition"/> fails. May be a
        /// literal string or a language-resource key resolved at API delivery time.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Message shown when the condition fails (literal or language-resource key).")]
        [DefaultValue("")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this rule is enabled.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Indicates whether this rule is enabled.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the evaluation order among rules sharing the same <see cref="Trigger"/>.
        /// Lower values are evaluated first.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Evaluation order among rules with the same trigger (lower first).")]
        [DefaultValue(0)]
        public int Order { get; set; } = 0;

        /// <summary>
        /// Gets the form schema that owns this rule.
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        [TreeNodeIgnore]
        public FormSchema? Schema
        {
            get { return (Collection as FormRuleCollection)?.Owner as FormSchema; }
        }

        /// <summary>
        /// Creates a deep copy of this instance. The result is unparented (no owning
        /// collection) — call sites typically add it via <c>Add(rule.Clone())</c>.
        /// </summary>
        public FormRule Clone()
        {
            return new FormRule
            {
                RuleId = RuleId,
                Trigger = Trigger,
                TargetTable = TargetTable,
                When = When,
                Condition = Condition,
                Message = Message,
                Enabled = Enabled,
                Order = Order,
            };
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{RuleId} - {Condition}";
        }
    }
}
