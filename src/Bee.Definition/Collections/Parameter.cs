using System.ComponentModel;
using Bee.Base;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// A parameter item.
    /// </summary>
    [DefaultProperty("Value")]
    public class Parameter : MessagePackKeyCollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="Parameter"/>.
        /// </summary>
        public Parameter()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="Parameter"/>.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        public Parameter(string name, object value)
        {
            this.Name = name;
            Value = value;
        }

        #endregion

        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string Name
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the parameter value.
        /// </summary>
        public object? Value { get; set; } = null;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return StringUtilities.Format("{0}={1}", this.Name, this.Value!);
        }
    }
}
