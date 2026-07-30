namespace Bee.Analyzers
{
    /// <summary>
    /// Diagnostic identifiers reported by the Bee.NET convention analyzers.
    /// </summary>
    /// <remarks>
    /// The numeric ranges group rules by the kind of convention they enforce, which also
    /// corresponds to the analysis pipeline each one uses:
    /// <list type="bullet">
    ///   <item><description>BEE1xxx: single definition file validation, read via AdditionalFiles.</description></item>
    ///   <item><description>BEE2xxx: cross-file consistency between definition files.</description></item>
    ///   <item><description>BEE3xxx: C# coding conventions.</description></item>
    ///   <item><description>BEE4xxx: serialisation and wire contract rules.</description></item>
    /// </list>
    /// </remarks>
    internal static class DiagnosticIds
    {
        /// <summary>
        /// A FormSchema declares a CategoryId that is not a valid database scope.
        /// </summary>
        public const string InvalidFormSchemaCategoryId = "BEE1001";

        /// <summary>
        /// A FormSchema table is not registered under the matching category in DbCategorySettings.
        /// </summary>
        public const string TableNotRegisteredInCategory = "BEE2001";

        /// <summary>
        /// A collection item constructor takes its parameters in an order that does not match
        /// the MessagePack key order.
        /// </summary>
        public const string ConstructorParameterOrderMismatch = "BEE4004";
    }
}
