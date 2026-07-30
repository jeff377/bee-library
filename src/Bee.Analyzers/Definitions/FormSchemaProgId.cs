namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Derives a form schema's program identifier from its file path.
    /// </summary>
    internal static class FormSchemaProgId
    {
        /// <summary>
        /// Gets the program identifier implied by the specified form schema file path.
        /// </summary>
        /// <param name="path">The path of a form schema definition file.</param>
        /// <returns>
        /// The file name with its directory and the form schema suffix removed, for example
        /// <c>Product</c> for <c>Define/FormSchema/Product.FormSchema.xml</c>.
        /// </returns>
        /// <remarks>
        /// Used as a fallback when the document carries no <c>ProgId</c> attribute, so that the
        /// diagnostic can still name the offending schema.
        /// </remarks>
        public static string FromPath(string path)
        {
            var separator = path.LastIndexOfAny(new[] { '/', '\\' });
            var fileName = separator >= 0 ? path.Substring(separator + 1) : path;

            var suffix = fileName.LastIndexOf(DefinitionFileNames.FormSchemaSuffix, StringComparison.OrdinalIgnoreCase);
            return suffix >= 0 ? fileName.Substring(0, suffix) : fileName;
        }
    }
}
