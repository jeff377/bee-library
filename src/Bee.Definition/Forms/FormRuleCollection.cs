using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A collection of business rules owned by a <see cref="FormSchema"/>.
    /// </summary>
    [Description("Business rule collection.")]
    [TreeNode("Rules", false)]
    public class FormRuleCollection : KeyCollectionBase<FormRule>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FormRuleCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public FormRuleCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="FormRuleCollection"/>.
        /// </summary>
        /// <param name="schema">The owning form schema.</param>
        public FormRuleCollection(FormSchema schema) : base(schema)
        { }
    }

    /// <summary>
    /// Convenience extension methods for <see cref="FormRuleCollection"/>.
    /// </summary>
    public static class FormRuleCollectionExtensions
    {
        /// <summary>
        /// Adds a rule to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="ruleId">The rule id.</param>
        /// <param name="condition">The condition that must hold for the rule to pass.</param>
        /// <param name="message">The message shown when the condition fails.</param>
        public static FormRule Add(this FormRuleCollection? collection, string ruleId, string condition, string message)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var rule = new FormRule(ruleId, condition, message);
            collection.Add(rule);
            return rule;
        }
    }
}
