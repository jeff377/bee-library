using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1003: a field declares a data type that is not a member of the framework's field type
    /// enumeration.
    /// </summary>
    /// <remarks>
    /// The attribute is deserialised into an enumeration by <c>XmlSerializer</c>, so an unknown value
    /// fails to deserialise and takes the whole definition file down with it — the resulting error names
    /// the file, not the offending attribute.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FieldDbTypeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidFieldDbType,
            title: "Field DbType must be a member of the field type enumeration",
            messageFormat: "Field '{0}' declares DbType '{1}', which is not a member of the framework "
                         + "field type enumeration. {2}.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Field data types are deserialised into an enumeration, so an unknown value "
                       + "prevents the entire definition file from loading.",
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

            // Both definition kinds carry the same enumeration on their fields and fail the same way.
            foreach (var schema in definitions.FormSchemas)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                Report(context, schema.Fields, schema.CreateLocation);
            }

            foreach (var table in definitions.TableSchemas)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                Report(context, table.Fields, table.CreateLocation);
            }
        }

        private static void Report(
            CompilationAnalysisContext context,
            IEnumerable<XElement> fields,
            Func<XAttribute, Location> createLocation)
        {
            foreach (var declaration in fields)
            {
                var dbType = declaration.Attribute("DbType");
                if (dbType is null || FieldDbTypes.IsValid(dbType.Value))
                    continue;

                var fieldName = declaration.Attribute("FieldName")?.Value ?? "(unnamed)";

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    createLocation(dbType),
                    fieldName,
                    dbType.Value,
                    BuildAdvice(dbType.Value)));
            }
        }

        private static string BuildAdvice(string value)
        {
            var casing = FieldDbTypes.FindCaseInsensitiveMatch(value);
            return casing is not null
                ? $"Enumeration deserialisation is case-sensitive. Fix: change it to '{casing}'"
                : "Accepted values are " + string.Join(", ", FieldDbTypes.All);
        }
    }
}
