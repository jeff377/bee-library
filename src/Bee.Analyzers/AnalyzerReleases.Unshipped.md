; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------------|----------|-------------------------------------------------------------------
BEE1001 | Bee.Definition | Error | FormSchemaCategoryIdAnalyzer — CategoryId must be an accepted database scope
BEE2001 | Bee.Definition | Warning | FormSchemaTableRegistrationAnalyzer — table must be registered under the declared scope
BEE4004 | Bee.Serialization | Error | MessagePackConstructorOrderAnalyzer — constructor parameters must follow integer key order
