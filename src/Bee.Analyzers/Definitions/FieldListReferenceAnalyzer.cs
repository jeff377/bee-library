using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1004: a comma separated field list names a field the schema does not declare.
    /// </summary>
    /// <remarks>
    /// These lists drive the grid columns and the lookup picker. An unknown entry is skipped rather than
    /// rejected, so the column simply never appears — a mistyped field name looks like a layout problem.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FieldListReferenceAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The schema-level attributes holding a comma separated list of field names.
        /// </summary>
        private static readonly string[] s_listAttributes = { "ListFields", "LookupFields" };

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.UnknownFieldListReference,
            title: "Field list must only reference declared fields",
            messageFormat: "FormSchema '{0}' lists '{1}' in {2}, but the schema declares no such field. "
                         + "Unknown entries are skipped silently, so the column or lookup value never "
                         + "appears. Fix: correct the name, or declare the field.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Comma separated field lists are resolved against the fields the schema declares. "
                       + "An entry that matches nothing is ignored without an error.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

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

                // Resolved against every field in the schema rather than per table: a master-detail
                // list legitimately mixes columns from the master and the detail tables.
                var declared = schema.DeclaredFieldNames;

                foreach (var attributeName in s_listAttributes)
                {
                    var attribute = schema.Root.Attribute(attributeName);
                    if (attribute is null || string.IsNullOrEmpty(attribute.Value))
                        continue;

                    foreach (var entry in FormSchemaModel.SplitFieldList(attribute.Value))
                    {
                        if (declared.Contains(entry))
                            continue;

                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule,
                            schema.CreateLocation(attribute),
                            schema.ProgId,
                            entry,
                            attributeName));
                    }
                }
            }
        }
    }
}
