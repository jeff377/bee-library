using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE2001: a form schema's physical table is not registered under the scope the schema
    /// declares in <c>DbCategorySettings.xml</c>.
    /// </summary>
    /// <remarks>
    /// The usual cause is a schema declaring the wrong <c>CategoryId</c> — a business table left on the
    /// common scope, for instance. Nothing detects this until the form is first used at run time,
    /// because the schema and the settings are separate files that are only brought together by the
    /// router.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FormSchemaTableRegistrationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.TableNotRegisteredInCategory,
            title: "FormSchema table must be registered under the declared database scope",
            messageFormat: "FormSchema '{0}' declares CategoryId '{1}', but its table '{2}' is not "
                         + "registered under that scope in DbCategorySettings.xml. {3}.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Every table a form reads or writes must be registered under the matching scope "
                       + "in the database category settings, otherwise it cannot be routed to a database.",
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
            var registry = DbCategoryRegistry.TryCreate(context.Options.AdditionalFiles, context.CancellationToken);
            if (registry is null)
                return;

            foreach (var file in context.Options.AdditionalFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (!DefinitionFileNames.IsFormSchema(file.Path))
                    continue;

                var text = file.GetText(context.CancellationToken);
                if (text is null)
                    continue;

                var root = DefinitionDocumentLoader.TryLoad(text)?.Root;
                if (root is null)
                    continue;

                var scope = root.Attribute("CategoryId")?.Value;

                // An unaccepted scope is BEE1001's subject. Reporting it here as well would give two
                // diagnostics for one mistake, and the registration check cannot succeed anyway.
                if (string.IsNullOrEmpty(scope) || !DbCategoryScopes.IsValid(scope!))
                    continue;

                var progId = root.Attribute("ProgId")?.Value ?? FormSchemaProgId.FromPath(file.Path);

                foreach (var table in root.Descendants("FormTable"))
                {
                    var dbTableName = table.Attribute("DbTableName");
                    if (dbTableName is null || string.IsNullOrEmpty(dbTableName.Value))
                        continue;

                    if (registry.IsRegistered(scope!, dbTableName.Value))
                        continue;

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        XmlAttributeLocator.Create(file.Path, text, dbTableName),
                        progId,
                        scope,
                        dbTableName.Value,
                        BuildAdvice(registry, dbTableName.Value, scope!)));
                }
            }
        }

        /// <summary>
        /// Builds the corrective advice appended to the diagnostic message.
        /// </summary>
        /// <param name="registry">The table registrations parsed from the settings.</param>
        /// <param name="tableName">The unregistered table name.</param>
        /// <param name="declaredScope">The scope the schema declares.</param>
        /// <returns>A sentence naming the concrete fix.</returns>
        private static string BuildAdvice(DbCategoryRegistry registry, string tableName, string declaredScope)
        {
            var actualScope = registry.FindScopeOf(tableName);
            if (actualScope is not null)
            {
                return $"It is registered under '{actualScope}'. Fix: either change the FormSchema "
                     + $"CategoryId to '{actualScope}', or move the table registration to "
                     + $"'{declaredScope}' in DbCategorySettings.xml";
            }

            return $"Fix: add a TableItem for '{tableName}' under the '{declaredScope}' category in "
                 + "DbCategorySettings.xml";
        }
    }
}
