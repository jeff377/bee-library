using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1005 and BEE1006: the two halves of a relation mapping that can disagree with the
    /// fields a schema declares.
    /// </summary>
    /// <remarks>
    /// Both rules share one traversal because they are two views of the same relationship: BEE1005
    /// catches a mapping writing into a field that does not exist, BEE1006 catches a field declared to
    /// receive a mapping that never arrives.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RelationMappingAnalyzer : DiagnosticAnalyzer
    {
        private const string RelationFieldType = "RelationField";

        private static readonly DiagnosticDescriptor UnknownDestination = new DiagnosticDescriptor(
            id: DiagnosticIds.UnknownMappingDestinationField,
            title: "Relation mapping must target a declared field",
            messageFormat: "FormSchema '{0}' has a relation mapping writing into '{1}', but the schema "
                         + "declares no such field. The mapped value is discarded at run time. "
                         + "Fix: declare '{1}' with Type=\"RelationField\", or correct the mapping.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A relation mapping copies a value from the referenced schema into a field of "
                       + "this one. A destination that does not exist silently drops the value.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        private static readonly DiagnosticDescriptor UnmappedRelationField = new DiagnosticDescriptor(
            id: DiagnosticIds.UnmappedRelationField,
            title: "Relation field should be populated by a mapping",
            messageFormat: "Field '{1}' in FormSchema '{0}' is declared as a relation field, but no "
                         + "FieldMapping writes into it, so it stays empty at run time. Fix: add a "
                         + "FieldMapping with DestinationField=\"{1}\", or remove the field.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Relation fields hold values copied from a referenced schema. One that no "
                       + "mapping populates is always empty.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(UnknownDestination, UnmappedRelationField);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationAction(Analyze);
        }

        private static void Analyze(CompilationAnalysisContext context)
        {
            var definitions = DefinitionContext.Create(context.Options.AdditionalFiles, context.CancellationToken);

            foreach (var schema in definitions.FormSchemas)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var declared = schema.DeclaredFieldNames;
                var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var mapping in schema.FieldMappings)
                {
                    var destination = mapping.Attribute("DestinationField");
                    if (destination is null || string.IsNullOrEmpty(destination.Value))
                        continue;

                    destinations.Add(destination.Value);

                    if (declared.Contains(destination.Value))
                        continue;

                    context.ReportDiagnostic(Diagnostic.Create(
                        UnknownDestination,
                        schema.CreateLocation(destination),
                        schema.ProgId,
                        destination.Value));
                }

                ReportUnmappedRelationFields(context, schema, destinations);
            }
        }

        private static void ReportUnmappedRelationFields(
            CompilationAnalysisContext context,
            FormSchemaModel schema,
            HashSet<string> destinations)
        {
            foreach (var declaration in schema.Fields)
            {
                if (!string.Equals(declaration.Attribute("Type")?.Value, RelationFieldType, StringComparison.Ordinal))
                    continue;

                var fieldName = declaration.Attribute("FieldName");
                if (fieldName is null || string.IsNullOrEmpty(fieldName.Value))
                    continue;

                if (destinations.Contains(fieldName.Value))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    UnmappedRelationField,
                    schema.CreateLocation(fieldName),
                    schema.ProgId,
                    fieldName.Value));
            }
        }
    }
}
