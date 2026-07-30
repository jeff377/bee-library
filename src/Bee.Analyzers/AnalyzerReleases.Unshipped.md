; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------------|----------|-------------------------------------------------------------------
BEE1001 | Bee.Definition | Error | FormSchemaCategoryIdAnalyzer — CategoryId must be an accepted database scope
BEE1002 | Bee.Definition | Error | DbCategorySettingsScopeAnalyzer — DbCategory Id must be an accepted database scope
BEE1003 | Bee.Definition | Error | FieldDbTypeAnalyzer — DbType must be a member of the field type enumeration
BEE1004 | Bee.Definition | Error | FieldListReferenceAnalyzer — field lists must only reference declared fields
BEE1005 | Bee.Definition | Error | RelationMappingAnalyzer — a relation mapping must target a declared field
BEE1006 | Bee.Definition | Warning | RelationMappingAnalyzer — a relation field should be populated by a mapping
BEE1007 | Bee.Definition | Error | DuplicateFieldNameAnalyzer — a table must not declare the same field twice
BEE2001 | Bee.Definition | Warning | FormSchemaTableRegistrationAnalyzer — table must be registered under the declared scope
BEE2002 | Bee.Definition | Warning | SidecarDefinitionAnalyzer — a table schema must exist under the folder matching the scope
BEE2003 | Bee.Definition | Error | RelationReferenceAnalyzer — a relation field must reference an existing FormSchema
BEE2004 | Bee.Definition | Error | RelationReferenceAnalyzer — a relation mapping must read a declared field
BEE2005 | Bee.Definition | Warning | SidecarDefinitionAnalyzer — a FormSchema should have a corresponding FormLayout
BEE2006 | Bee.Definition | Warning | PersistedFieldAnalyzer — a persisted field must exist in the table schema
BEE2007 | Bee.Definition | Info | LanguageCoverageAnalyzer — cultures should cover the same translation keys
BEE3001 | Bee.Business | Warning | BusinessObjectAccessControlAnalyzer — a business object API method must declare access control
BEE3002 | Bee.Definition | Warning | DefinitionCollectionPropertyAnalyzer — a framework collection property must use a framework collection type
BEE4001 | Bee.Serialization | Error | CollectionFormatterRegistrationAnalyzer — a MessagePack collection must be registered with a formatter
BEE4002 | Bee.Serialization | Error | WireFieldNameAnalyzer — a JSON rename must not conflict with name-based MessagePack keys
BEE4003 | Bee.Serialization | Error | UnionKeyStrategyAnalyzer — a union hierarchy must use integer MessagePack keys
BEE4004 | Bee.Serialization | Error | MessagePackConstructorOrderAnalyzer — constructor parameters must follow integer key order
BEE4005 | Bee.Serialization | Warning | CollectionAddOverloadAnalyzer — a framework collection should expose a single public Add
BEE4006 | Bee.Serialization | Error | ParameterlessConstructorAnalyzer — a serialized type must have a public parameterless constructor
