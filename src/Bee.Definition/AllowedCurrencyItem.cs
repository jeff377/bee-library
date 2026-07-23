using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition
{
    /// <summary>
    /// A single entry in a company's allowed-currency whitelist, held in
    /// <see cref="CompanyAllowedCurrencies"/>. Holds one ISO 4217 alpha-3 currency code.
    /// </summary>
    [Description("Company allowed-currency whitelist item.")]
    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class AllowedCurrencyItem : MessagePackCollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AllowedCurrencyItem"/>.
        /// </summary>
        public AllowedCurrencyItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="AllowedCurrencyItem"/>.
        /// </summary>
        /// <param name="code">The ISO 4217 alpha-3 currency code.</param>
        public AllowedCurrencyItem(string code)
        {
            Code = code;
        }

        /// <summary>
        /// Gets or sets the ISO 4217 alpha-3 currency code.
        /// </summary>
        [XmlAttribute]
        public string Code { get; set; } = string.Empty;
    }
}
