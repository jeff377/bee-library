using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1007: a table declares the same field name twice.
    /// </summary>
    /// <remarks>
    /// Fields are loaded into a keyed collection, so a duplicate does not raise an error — one
    /// declaration simply replaces the other. Whichever attributes the losing declaration carried
    /// (caption, data type, visibility) are silently discarded.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DuplicateFieldNameAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.DuplicateFieldName,
            title: "Table must not declare the same field name twice",
            messageFormat: "Table '{0}' declares field '{1}' more than once. Fields load into a keyed "
                         + "collection, so one declaration silently replaces the other along with its "
                         + "attributes. Fix: remove or rename the duplicate.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Duplicate field names are resolved by replacement rather than rejected, so the "
                       + "attributes of the discarded declaration are lost without any error.",
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

                // Scoped per table, not per schema: a master-detail schema legitimately repeats
                // sys_id and sys_name across its tables.
                foreach (var table in schema.Tables)
                {
                    var tableName = table.Attribute("TableName")?.Value ?? schema.ProgId;
                    Report(context, tableName, table.Descendants("FormField"), schema.CreateLocation);
                }
            }

            foreach (var table in definitions.TableSchemas)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                Report(context, table.TableName, table.Fields, table.CreateLocation);
            }
        }

        private static void Report(
            CompilationAnalysisContext context,
            string tableName,
            IEnumerable<XElement> fields,
            Func<XAttribute, Location> createLocation)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var declaration in fields)
            {
                var fieldName = declaration.Attribute("FieldName");
                if (fieldName is null || string.IsNullOrEmpty(fieldName.Value))
                    continue;

                // Reported on the later declaration: that is the one to delete, and the earlier one
                // stays a valid reference point for the reader.
                if (seen.Add(fieldName.Value))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    createLocation(fieldName),
                    tableName,
                    fieldName.Value));
            }
        }
    }
}
