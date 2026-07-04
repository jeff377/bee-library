namespace Bee.Definition.Forms
{
    /// <summary>
    /// The data-sensitivity classification of a form field, used as an opt-in gate for
    /// field-level permission degradation. Parallels <see cref="ScopeRole"/>: a named,
    /// finite classification the form designer picks rather than inventing an id.
    /// </summary>
    /// <remarks>
    /// Each non-<see cref="None"/> value maps by convention to a well-known permission model
    /// (see <see cref="SensitiveCategoryExtensions.ToPermissionModelId"/>): the model gates the
    /// field's visibility (<c>Read</c>) and editability (<c>Update</c>). Classification is a
    /// company-wide, cross-form data concern — the same <see cref="Cost"/> gate applies to every
    /// form that marks a cost field. The default <see cref="None"/> means the field is not
    /// permission-controlled and always renders per its layout.
    /// </remarks>
    public enum SensitiveCategory
    {
        /// <summary>The field carries no sensitivity classification (not controlled).</summary>
        None,

        /// <summary>Monetary amount data (gated by the well-known <c>Amount</c> model).</summary>
        Amount,

        /// <summary>Cost data (gated by the well-known <c>Cost</c> model).</summary>
        Cost,

        /// <summary>Personal data / PII (gated by the well-known <c>PersonalData</c> model).</summary>
        PersonalData
    }
}
