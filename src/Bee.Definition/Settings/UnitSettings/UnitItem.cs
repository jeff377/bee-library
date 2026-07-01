using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A single system-level unit-of-measure definition (SAP T006-style), held in
    /// <see cref="UnitSettings"/>. Unit decimals are system-wide reference data (KG = 3, PCS = 0),
    /// independent of company; quantities and weights resolve their decimals from the bound unit.
    /// </summary>
    [Description("System-level unit-of-measure definition item.")]
    [MessagePackObject]
    public sealed class UnitItem : MessagePackCollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnitItem"/>.
        /// </summary>
        public UnitItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="UnitItem"/>. The parameter order matches the
        /// <c>[Key]</c> order (<c>Code</c>, <c>Decimals</c>, <c>Dimension</c>, <c>Name</c>) so
        /// MessagePack's constructor-based deserialization maps values to the right members.
        /// </summary>
        /// <param name="code">The unit code (the key), for example <c>KG</c> or <c>PCS</c>.</param>
        /// <param name="decimals">The display decimal places for this unit.</param>
        /// <param name="dimension">The dimension grouping (optional), for example <c>weight</c>.</param>
        /// <param name="name">The display name.</param>
        public UnitItem(string code, int decimals, string dimension = "", string name = "")
        {
            Code = code;
            Decimals = decimals;
            Dimension = dimension;
            Name = name;
        }

        /// <summary>
        /// Gets or sets the unit code (for example <c>KG</c>, <c>G</c>, <c>PCS</c>, <c>L</c>).
        /// This is the lookup key.
        /// </summary>
        [XmlAttribute]
        [Key(100)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display decimal places for this unit (SAP T006 <c>ANDEC</c>; for example
        /// <c>KG</c> = 3, <c>PCS</c> = 0).
        /// </summary>
        [XmlAttribute]
        [Key(101)]
        public int Decimals { get; set; }

        /// <summary>
        /// Gets or sets the dimension grouping (optional), for example <c>weight</c> / <c>length</c> /
        /// <c>volume</c> / <c>count</c>. Used for UI grouping only.
        /// </summary>
        [XmlAttribute]
        [Key(102)]
        public string Dimension { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name (for example <c>Kilogram</c>).
        /// </summary>
        [XmlAttribute]
        [Key(103)]
        public string Name { get; set; } = string.Empty;
    }
}
