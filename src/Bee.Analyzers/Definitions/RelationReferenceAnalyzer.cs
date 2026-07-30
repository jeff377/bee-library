using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE2003 and BEE2004: relation references that point at a schema or a field which does not
    /// exist.
    /// </summary>
    /// <remarks>
    /// A relation field names another schema and copies fields out of it. Neither half is verified until
    /// the form is opened and the lookup runs, so a typo in either surfaces as an empty picker rather
    /// than an error.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RelationReferenceAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor UnknownProgId = new DiagnosticDescriptor(
            id: DiagnosticIds.UnknownRelationProgId,
            title: "Relation field must reference an existing FormSchema",
            messageFormat: "Field '{1}' in FormSchema '{0}' references RelationProgId '{2}', but no "
                         + "FormSchema declares it. The lookup resolves to nothing at run time. "
                         + "Fix: correct the ProgId, or add the referenced schema.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A relation field resolves its lookup through the referenced program identifier. "
                       + "One that no schema declares cannot be resolved.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        private static readonly DiagnosticDescriptor UnknownSourceField = new DiagnosticDescriptor(
            id: DiagnosticIds.UnknownMappingSourceField,
            title: "Relation mapping must read a field the referenced schema declares",
            messageFormat: "FormSchema '{0}' maps SourceField '{2}' from '{1}', but that schema declares "
                         + "no such field, so the mapped destination stays empty. "
                         + "Fix: correct the field name, or declare it in '{1}'.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A relation mapping reads a field out of the referenced schema. A source field "
                       + "that does not exist there yields no value.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(UnknownProgId, UnknownSourceField);

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

                foreach (var declaration in schema.Fields)
                {
                    var relationProgId = declaration.Attribute("RelationProgId");
                    if (relationProgId is null || string.IsNullOrEmpty(relationProgId.Value))
                        continue;

                    // Framework-supplied schemas are embedded resources rather than files, so their
                    // absence from the additional files says nothing about whether they exist.
                    if (FrameworkProgIds.IsFrameworkSupplied(relationProgId.Value))
                        continue;

                    var referenced = definitions.FindByProgId(relationProgId.Value);
                    if (referenced is null)
                    {
                        var fieldName = declaration.Attribute("FieldName")?.Value ?? "(unnamed)";
                        context.ReportDiagnostic(Diagnostic.Create(
                            UnknownProgId,
                            schema.CreateLocation(relationProgId),
                            schema.ProgId,
                            fieldName,
                            relationProgId.Value));
                        continue;
                    }

                    ReportUnknownSourceFields(context, schema, declaration, referenced);
                }
            }
        }

        private static void ReportUnknownSourceFields(
            CompilationAnalysisContext context,
            FormSchemaModel schema,
            System.Xml.Linq.XElement declaration,
            FormSchemaModel referenced)
        {
            var declaredThere = referenced.DeclaredFieldNames;

            foreach (var mapping in declaration.Descendants("FieldMapping"))
            {
                var sourceField = mapping.Attribute("SourceField");
                if (sourceField is null || string.IsNullOrEmpty(sourceField.Value))
                    continue;

                if (declaredThere.Contains(sourceField.Value))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    UnknownSourceField,
                    schema.CreateLocation(sourceField),
                    schema.ProgId,
                    referenced.ProgId,
                    sourceField.Value));
            }
        }
    }
}
