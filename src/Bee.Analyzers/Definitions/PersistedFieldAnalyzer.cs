using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE2006: a form schema declares a persisted field that the corresponding table schema has
    /// no column for.
    /// </summary>
    /// <remarks>
    /// The form schema and the table schema are separate files that only meet when a query is built.
    /// A field with no column behind it produces a database error naming the column, with nothing to
    /// connect it back to the schema that asked for it.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PersistedFieldAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The field type that denotes a column in the database. Relation and virtual fields are
        /// populated in memory and deliberately have no column.
        /// </summary>
        private const string PersistedFieldType = "DbField";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.FieldMissingFromTableSchema,
            title: "Persisted field must exist in the table schema",
            messageFormat: "FormSchema '{0}' declares persisted field '{1}' on table '{2}', but the table "
                         + "schema has no such column. Queries touching that field fail at run time. "
                         + "Fix: add the column to the table schema, or mark the field "
                         + "Type=\"VirtualField\" if it is not persisted.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A form schema field without a matching table schema column has no storage. The "
                       + "mismatch is only discovered when a query built from the schema reaches the "
                       + "database.",
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

                var scope = schema.CategoryId;
                if (string.IsNullOrEmpty(scope) || !DbCategoryScopes.IsValid(scope!))
                    continue;

                foreach (var table in schema.Tables)
                {
                    var dbTableName = table.Attribute("DbTableName")?.Value;
                    if (string.IsNullOrEmpty(dbTableName))
                        continue;

                    // A missing table schema is BEE2002's subject; without it there is nothing to
                    // compare against and every field would be reported.
                    var tableSchema = definitions.FindTableSchema(scope!, dbTableName!);
                    if (tableSchema is null)
                        continue;

                    ReportMissingColumns(context, schema, table.Descendants("FormField"), dbTableName!, tableSchema);
                }
            }
        }

        private static void ReportMissingColumns(
            CompilationAnalysisContext context,
            FormSchemaModel schema,
            IEnumerable<System.Xml.Linq.XElement> fields,
            string dbTableName,
            TableSchemaModel tableSchema)
        {
            var columns = tableSchema.DeclaredFieldNames;

            foreach (var declaration in fields)
            {
                // An absent Type attribute means a persisted field, so the default has to be treated
                // as persisted rather than skipped.
                var fieldType = declaration.Attribute("Type")?.Value;
                if (!string.IsNullOrEmpty(fieldType) &&
                    !string.Equals(fieldType, PersistedFieldType, StringComparison.Ordinal))
                {
                    continue;
                }

                var fieldName = declaration.Attribute("FieldName");
                if (fieldName is null || string.IsNullOrEmpty(fieldName.Value))
                    continue;

                if (columns.Contains(fieldName.Value))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    schema.CreateLocation(fieldName),
                    schema.ProgId,
                    fieldName.Value,
                    dbTableName));
            }
        }
    }
}
