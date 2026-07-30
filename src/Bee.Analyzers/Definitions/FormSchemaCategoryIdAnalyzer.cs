using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1001: a form schema declares a <c>CategoryId</c> that is not an accepted database scope.
    /// </summary>
    /// <remarks>
    /// <c>CategoryId</c> selects which database a form's tables live in. An unaccepted value cannot be
    /// routed, so the failure only ever surfaces at run time when the form is first used.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FormSchemaCategoryIdAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.InvalidFormSchemaCategoryId,
            title: "FormSchema CategoryId must be an accepted database scope",
            messageFormat: "FormSchema '{0}' declares CategoryId '{1}', which is not an accepted database scope. {2}.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "CategoryId selects the database scope a form's tables are routed to. "
                       + "Only the built-in scopes are accepted, and business tables must use the company scope.",
            helpLinkUri: null,
            // NOTE: Definition files are only reachable from a compilation action, which makes every
            // diagnostic here a compilation-end diagnostic (RS1037). Consequence: IDEs do not surface
            // these live while typing unless full solution analysis is enabled. Build output — which
            // is what matters for the AI-assisted workflow this analyzer targets — is unaffected.
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            // Definition files are AdditionalFiles, which are only reachable from a compilation action.
            context.RegisterCompilationAction(Analyze);
        }

        private static void Analyze(CompilationAnalysisContext context)
        {
            foreach (var file in context.Options.AdditionalFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (!DefinitionFileNames.IsFormSchema(file.Path))
                    continue;

                var text = file.GetText(context.CancellationToken);
                if (text is null)
                    continue;

                var document = DefinitionDocumentLoader.TryLoad(text);
                var root = document?.Root;
                if (root is null)
                    continue;

                var categoryId = root.Attribute("CategoryId");
                if (categoryId is null)
                    continue;

                var value = categoryId.Value;
                if (DbCategoryScopes.IsValid(value))
                    continue;

                var progId = root.Attribute("ProgId")?.Value ?? FormSchemaProgId.FromPath(file.Path);
                var advice = BuildAdvice(value);

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    XmlAttributeLocator.Create(file.Path, text, categoryId),
                    progId,
                    value,
                    advice));
            }
        }

        /// <summary>
        /// Builds the corrective advice appended to the diagnostic message.
        /// </summary>
        /// <param name="value">The rejected scope value.</param>
        /// <returns>A sentence naming the concrete fix.</returns>
        private static string BuildAdvice(string value)
        {
            var casing = DbCategoryScopes.FindCaseInsensitiveMatch(value);
            if (casing is not null)
            {
                return "Scope comparison is ordinal, so casing must match exactly. "
                     + $"Fix: change it to '{casing}'";
            }

            return $"Accepted values are {DbCategoryScopes.ToDisplayList()}. "
                 + $"Business tables must use '{DbCategoryScopes.Company}'; "
                 + $"'{DbCategoryScopes.Common}' is reserved for shared framework tables";
        }
    }
}
