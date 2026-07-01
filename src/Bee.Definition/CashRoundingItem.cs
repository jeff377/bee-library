using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition
{
    /// <summary>
    /// A company-level cash-rounding override for a single currency (SAP T001R-style), held in
    /// <see cref="CompanyCashRounding"/>. The unit (for example <c>0.05</c>) applies only to the
    /// final payable amount of a document, distinct from the currency's natural minor unit.
    /// </summary>
    [Description("Company cash-rounding override item.")]
    [MessagePackObject]
    public sealed class CashRoundingItem : MessagePackCollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CashRoundingItem"/>.
        /// </summary>
        public CashRoundingItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="CashRoundingItem"/>.
        /// </summary>
        /// <param name="currencyCode">The ISO 4217 alpha-3 currency code this override applies to.</param>
        /// <param name="unit">The cash-rounding unit (for example <c>0.05</c>).</param>
        public CashRoundingItem(string currencyCode, decimal unit)
        {
            CurrencyCode = currencyCode;
            Unit = unit;
        }

        /// <summary>
        /// Gets or sets the ISO 4217 alpha-3 currency code this override applies to.
        /// </summary>
        [XmlAttribute]
        [Key(100)]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cash-rounding unit applied to the final payable amount (for example
        /// <c>0.05</c> to round to the nearest five cents).
        /// </summary>
        [XmlAttribute]
        [Key(101)]
        public decimal Unit { get; set; }
    }
}
