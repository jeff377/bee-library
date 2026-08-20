using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE2002 and BEE2005: the sidecar definition files a form schema needs but that are
    /// missing or filed in the wrong folder.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Both rules stay silent unless the corresponding definition kind is present in the
    /// additional files at all. Definitions can be stored in the database instead of the file system
    /// (see <c>DbDefineStorage</c>), and a consumer may deliberately supply only part of their
    /// definitions to the compiler. Without that guard the rules would fire on every schema of every
    /// such project.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SidecarDefinitionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor MissingTableSchema = new DiagnosticDescriptor(
            id: DiagnosticIds.MissingTableSchema,
            title: "FormSchema table must have a table schema under the matching scope folder",
            messageFormat: "FormSchema '{0}' maps to table '{1}', but no table schema was found at "
                         + "TableSchema/{2}/{1}.TableSchema.xml. {3}.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Table schemas are resolved by scope folder and table name. A schema filed under "
                       + "a different folder than the CategoryId is not found even though the file exists.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        private static readonly DiagnosticDescriptor MissingFormLayout = new DiagnosticDescriptor(
            id: DiagnosticIds.MissingFormLayout,
            title: "FormSchema must have a corresponding FormLayout",
            messageFormat: "FormSchema '{0}' has no corresponding layout at "
                         + "FormLayout/{0}.FormLayout.xml, so opening the form fails at run time. "
                         + "Fix: author the layout file at design time (a definition editor can "
                         + "generate a starting point from the schema).",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A form schema drives both the database and the user interface. The layout is "
                       + "authored at design time and the run time renders it as stored — it is never "
                       + "generated on the fly — so a schema without one defines data that no screen "
                       + "can present.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(MissingTableSchema, MissingFormLayout);

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

                if (definitions.HasTableSchemaFiles)
                    ReportMissingTableSchemas(context, definitions, schema);

                if (definitions.HasFormLayoutFiles && !definitions.HasFormLayout(schema.ProgId))
                {
                    var progId = schema.Root.Attribute("ProgId");
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingFormLayout,
                        progId is not null ? schema.CreateLocation(progId) : Location.None,
                        schema.ProgId));
                }
            }
        }

        private static void ReportMissingTableSchemas(
            CompilationAnalysisContext context,
            DefinitionContext definitions,
            FormSchemaModel schema)
        {
            var scope = schema.CategoryId;

            // An unaccepted scope is BEE1001's subject; the folder lookup cannot succeed either way.
            if (string.IsNullOrEmpty(scope) || !DbCategoryScopes.IsValid(scope!))
                return;

            foreach (var table in schema.Tables)
            {
                var dbTableName = table.Attribute("DbTableName");
                if (dbTableName is null || string.IsNullOrEmpty(dbTableName.Value))
                    continue;

                if (definitions.FindTableSchema(scope!, dbTableName.Value) is not null)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    MissingTableSchema,
                    schema.CreateLocation(dbTableName),
                    schema.ProgId,
                    dbTableName.Value,
                    scope,
                    BuildAdvice(definitions, dbTableName.Value, scope!)));
            }
        }

        /// <summary>
        /// Builds the corrective advice, naming the folder a matching schema was actually filed under.
        /// </summary>
        /// <param name="definitions">The indexed definition files.</param>
        /// <param name="tableName">The table with no schema under the expected scope.</param>
        /// <param name="declaredScope">The scope the form schema declares.</param>
        /// <returns>A sentence naming the concrete fix.</returns>
        private static string BuildAdvice(DefinitionContext definitions, string tableName, string declaredScope)
        {
            foreach (var candidate in definitions.TableSchemas)
            {
                if (candidate.CategoryId is null ||
                    string.Equals(candidate.CategoryId, declaredScope, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(candidate.TableName, tableName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return $"One exists under '{candidate.CategoryId}' instead. The folder must match the "
                     + $"CategoryId. Fix: move it to TableSchema/{declaredScope}/, or change the "
                     + $"FormSchema CategoryId to '{candidate.CategoryId}'";
            }

            return "Fix: add the table schema, or correct DbTableName";
        }
    }
}
