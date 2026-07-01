namespace Bee.Definition
{
    /// <summary>
    /// The source that determines the decimal places for a <see cref="NumberKind"/>.
    /// </summary>
    /// <remarks>
    /// The four members are the final contract from core onward. The core increment has no
    /// currency or unit settings yet, so its bake/round logic treats <see cref="Currency"/>
    /// and <see cref="Unit"/> as falling back to <see cref="Company"/>; the multi-currency
    /// and unit-of-measure increments replace those fallbacks with real reference-field
    /// resolution without changing this enum.
    /// </remarks>
    public enum DecimalsSource
    {
        /// <summary>Company override table (<c>CompanyNumberFormats</c>), falling back to the framework default.</summary>
        Company = 0,

        /// <summary>Bound currency key field (SAP CUKY); falls back to company when no currency is resolved.</summary>
        Currency,

        /// <summary>Bound unit-of-measure field (SAP UNIT); falls back to company when no unit is resolved.</summary>
        Unit,

        /// <summary>System-fixed framework default, independent of company or reference field.</summary>
        SystemFixed,
    }
}
