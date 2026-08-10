using Bee.Base.Collections;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Definition
{
    /// <summary>
    /// A company-level decimal-places override for a single <see cref="NumberKind"/>.
    /// Held in <see cref="CompanyNumberFormats"/>.
    /// </summary>
    [Description("Company number-format override item.")]
    public sealed class NumberFormatItem : CollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NumberFormatItem"/>.
        /// </summary>
        public NumberFormatItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="NumberFormatItem"/>.
        /// </summary>
        /// <param name="kind">The number kind this override applies to.</param>
        /// <param name="decimals">The decimal places for this kind.</param>
        public NumberFormatItem(NumberKind kind, int decimals)
        {
            Kind = kind;
            Decimals = decimals;
        }

        /// <summary>
        /// Gets or sets the number kind this override applies to.
        /// </summary>
        [XmlAttribute]
        public NumberKind Kind { get; set; } = NumberKind.None;

        /// <summary>
        /// Gets or sets the decimal places for this kind.
        /// </summary>
        [XmlAttribute]
        public int Decimals { get; set; }
    }
}
