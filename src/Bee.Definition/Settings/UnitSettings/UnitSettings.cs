using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// System-level unit-of-measure master (SAP T006-style): a curated table of <see cref="UnitItem"/>
    /// carrying each unit's display decimals. Unit decimals are system-wide and independent of company;
    /// quantities and weights bind a unit field and resolve their decimals from the bound unit's value.
    /// Persisted as a singleton define through <c>IDefineStorage</c> (file or DB) and shipped to the
    /// client so the UI can resolve quantity/weight decimals at runtime.
    /// </summary>
    /// <remarks>
    /// Parallels <see cref="CurrencySettings"/>. Uses <see cref="MessagePackCollectionBase{T}"/> so the
    /// table travels over the MessagePack wire cleanly; <c>MessagePackCodec</c> registers
    /// <c>CollectionBaseFormatter&lt;UnitSettings, UnitItem&gt;</c>. Keyed-lookup semantics are provided
    /// by <see cref="Find"/>.
    /// </remarks>
    [Description("System-level unit-of-measure master.")]
    [MessagePackObject]
    [XmlRoot("UnitSettings")]
    public class UnitSettings : MessagePackCollectionBase<UnitItem>
    {
        /// <summary>The fallback decimals used when a unit code is not found.</summary>
        public const int FallbackDecimals = 0;

        /// <summary>
        /// Initializes a new instance of <see cref="UnitSettings"/>.
        /// </summary>
        public UnitSettings()
        { }

        /// <summary>
        /// Finds the unit item for the specified code, or <c>null</c> when the code is not defined.
        /// Matching is case-insensitive.
        /// </summary>
        /// <param name="code">The unit code.</param>
        public UnitItem? Find(string code)
        {
            if (string.IsNullOrEmpty(code)) { return null; }
            return this.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the display decimal places for the specified unit code, or <see cref="FallbackDecimals"/>
        /// (<c>0</c>) when the code is not defined.
        /// </summary>
        /// <param name="code">The unit code.</param>
        public int GetDecimals(string code)
        {
            return Find(code)?.Decimals ?? FallbackDecimals;
        }
    }
}
