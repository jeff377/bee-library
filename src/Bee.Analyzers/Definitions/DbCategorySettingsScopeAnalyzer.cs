using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1002: a database category declares an identifier that is not an accepted scope.
    /// </summary>
    /// <remarks>
    /// The framework routes on these three scopes only. A category under any other identifier registers
    /// tables that nothing can ever resolve, and the tables appear simply to be unregistered.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DbCategorySettingsScopeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidDbCategoryId,
            title: "DbCategory identifier must be an accepted database scope",
            messageFormat: "DbCategorySettings declares category '{0}', which is not an accepted database "
                         + "scope. {1}.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Database categories are limited to the built-in scopes the framework routes on. "
                       + "Tables registered under any other identifier can never be resolved.",
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
            foreach (var file in context.Options.AdditionalFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (!DefinitionFileNames.IsDbCategorySettings(file.Path))
                    continue;

                var text = file.GetText(context.CancellationToken);
                if (text is null)
                    continue;

                var root = DefinitionDocumentLoader.TryLoad(text)?.Root;
                if (root is null)
                    continue;

                foreach (var category in root.Descendants("DbCategory"))
                {
                    var id = category.Attribute("Id");
                    if (id is null || DbCategoryScopes.IsValid(id.Value))
                        continue;

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        XmlAttributeLocator.Create(file.Path, text, id),
                        id.Value,
                        BuildAdvice(id.Value)));
                }
            }
        }

        private static string BuildAdvice(string value)
        {
            var casing = DbCategoryScopes.FindCaseInsensitiveMatch(value);
            return casing is not null
                ? $"Scope comparison is ordinal, so casing must match exactly. Fix: change it to '{casing}'"
                : $"Accepted values are {DbCategoryScopes.ToDisplayList()}";
        }
    }
}
