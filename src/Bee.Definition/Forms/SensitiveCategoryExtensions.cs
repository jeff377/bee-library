namespace Bee.Definition.Forms
{
    /// <summary>
    /// Extension helpers for <see cref="SensitiveCategory"/>.
    /// </summary>
    public static class SensitiveCategoryExtensions
    {
        /// <summary>
        /// Returns the well-known permission model id that gates the given sensitivity
        /// classification. By convention the model id equals the category name
        /// (e.g. <see cref="SensitiveCategory.Cost"/> maps to <c>"Cost"</c>); this switch is
        /// the single source binding categories to model ids, so both the load-time validator
        /// and the client-side capability resolver stay aligned. Returns an empty string for
        /// <see cref="SensitiveCategory.None"/> (not controlled).
        /// </summary>
        /// <param name="category">The sensitivity classification.</param>
        public static string ToPermissionModelId(this SensitiveCategory category)
            => category switch
            {
                SensitiveCategory.Amount => "Amount",
                SensitiveCategory.Cost => "Cost",
                SensitiveCategory.PersonalData => "PersonalData",
                _ => string.Empty
            };
    }
}
