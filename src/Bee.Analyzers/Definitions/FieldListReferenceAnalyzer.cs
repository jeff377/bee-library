using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1004: a comma separated field list names a field the schema does not declare.
    /// </summary>
    /// <remarks>
    /// These lists drive the grid columns and the lookup picker, and the two attributes fail differently
    /// at run time: an unknown <c>LookupFields</c> entry is skipped, so the value simply never appears,
    /// whereas an unknown <c>ListFields</c> entry is dropped by the layout generator but still reaches the
    /// SELECT builder, which throws. The message therefore names the consequence per attribute.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FieldListReferenceAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The schema-level attributes holding a comma separated list of field names, paired with the
        /// run-time consequence of an unknown entry in that attribute.
        /// </summary>
        private static readonly (string Name, string Consequence)[] s_listAttributes =
        {
            ("ListFields",
                "The list layout drops the unknown column, but the query keeps it: building the SELECT "
              + "throws InvalidOperationException at run time."),
            ("LookupFields",
                "The unknown entry is skipped silently, so that lookup value never appears."),
        };

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.UnknownFieldListReference,
            title: "Field list must only reference declared fields",
            messageFormat: "FormSchema '{0}' lists '{1}' in {2}, but the schema declares no such field. "
                         + "{3} Fix: correct the name, or declare the field.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Comma separated field lists are resolved against the fields the schema declares. "
                       + "An unknown LookupFields entry is ignored without an error, while an unknown "
                       + "ListFields entry is dropped from the layout yet still fails the SELECT it builds.",
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

                foreach (var (attributeName, consequence) in s_listAttributes)
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
                            attributeName,
                            consequence));
                    }
                }
            }
        }
    }
}
