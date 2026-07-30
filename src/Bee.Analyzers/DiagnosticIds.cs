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
        /// A DbCategorySettings category declares an identifier that is not a valid database scope.
        /// </summary>
        public const string InvalidDbCategoryId = "BEE1002";

        /// <summary>
        /// A field declares a data type that is not a member of the framework's field type enumeration.
        /// </summary>
        public const string InvalidFieldDbType = "BEE1003";

        /// <summary>
        /// A comma separated field list references a field the schema does not declare.
        /// </summary>
        public const string UnknownFieldListReference = "BEE1004";

        /// <summary>
        /// A relation field mapping targets a field the schema does not declare.
        /// </summary>
        public const string UnknownMappingDestinationField = "BEE1005";

        /// <summary>
        /// A field is marked as a relation field but no mapping populates it.
        /// </summary>
        public const string UnmappedRelationField = "BEE1006";

        /// <summary>
        /// A table declares the same field name more than once.
        /// </summary>
        public const string DuplicateFieldName = "BEE1007";

        /// <summary>
        /// A FormSchema table is not registered under the matching category in DbCategorySettings.
        /// </summary>
        public const string TableNotRegisteredInCategory = "BEE2001";

        /// <summary>
        /// A FormSchema table has no table schema under the folder matching its declared scope.
        /// </summary>
        public const string MissingTableSchema = "BEE2002";

        /// <summary>
        /// A relation field references a program identifier that no FormSchema declares.
        /// </summary>
        public const string UnknownRelationProgId = "BEE2003";

        /// <summary>
        /// A relation field mapping reads a field the referenced schema does not declare.
        /// </summary>
        public const string UnknownMappingSourceField = "BEE2004";

        /// <summary>
        /// A FormSchema has no corresponding FormLayout.
        /// </summary>
        public const string MissingFormLayout = "BEE2005";

        /// <summary>
        /// A persisted FormSchema field is absent from the corresponding table schema.
        /// </summary>
        public const string FieldMissingFromTableSchema = "BEE2006";

        /// <summary>
        /// A language resource key is present in some cultures but missing from others.
        /// </summary>
        public const string InconsistentLanguageCoverage = "BEE2007";

        /// <summary>
        /// A public method on a business object declares no API access control.
        /// </summary>
        public const string MissingApiAccessControl = "BEE3001";

        /// <summary>
        /// A definition type exposes a collection property that is not a framework collection.
        /// </summary>
        public const string NonFrameworkCollectionProperty = "BEE3002";

        /// <summary>
        /// A collection deriving from a framework collection base is not registered with a formatter.
        /// </summary>
        public const string CollectionFormatterNotRegistered = "BEE4001";

        /// <summary>
        /// A name-based MessagePack type renames a property for JSON only, so the two wire formats
        /// disagree on the field name.
        /// </summary>
        public const string WireFieldNameMismatch = "BEE4002";

        /// <summary>
        /// A type in a polymorphic union hierarchy opts into name-based keys instead of integer keys.
        /// </summary>
        public const string UnionMustUseIntegerKeys = "BEE4003";

        /// <summary>
        /// A collection item constructor takes its parameters in an order that does not match
        /// the MessagePack key order.
        /// </summary>
        public const string ConstructorParameterOrderMismatch = "BEE4004";

        /// <summary>
        /// A collection declares an additional public Add overload, which reflection-only
        /// serialization cannot resolve unambiguously.
        /// </summary>
        public const string AmbiguousCollectionAdd = "BEE4005";

        /// <summary>
        /// A serialized type has no parameterless constructor, so deserialization cannot create it.
        /// </summary>
        public const string MissingParameterlessConstructor = "BEE4006";
    }
}
