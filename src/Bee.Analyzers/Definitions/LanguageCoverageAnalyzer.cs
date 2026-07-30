using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE2007: a language resource is missing keys that its sibling cultures translate.
    /// </summary>
    /// <remarks>
    /// A missing key is not an error at run time — it falls back to another culture — so an untranslated
    /// caption shows up in the wrong language rather than as a failure. That makes it easy to ship and
    /// hard to notice, but also legitimate in some projects, hence the informational severity.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LanguageCoverageAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// How many missing keys to name before summarising the rest.
        /// </summary>
        private const int SampleSize = 3;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.InconsistentLanguageCoverage,
            title: "Language resources should cover the same keys across cultures",
            messageFormat: "Language resource '{0}' for culture '{1}' is missing {2} key(s) that other "
                         + "cultures translate ({3}). Missing keys fall back to another culture rather "
                         + "than failing, so the caption appears in the wrong language. "
                         + "Fix: add the missing entries.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Translation keys resolved through a fallback culture render in the wrong language "
                       + "without raising an error.",
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

            foreach (var group in definitions.LanguageResources.GroupBy(
                resource => resource.Namespace, StringComparer.OrdinalIgnoreCase))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var resources = group.ToList();

                // Nothing to compare against with a single culture: every key it has is by definition
                // the complete set.
                if (resources.Count < 2)
                    continue;

                var union = new HashSet<string>(StringComparer.Ordinal);
                foreach (var resource in resources)
                    union.UnionWith(resource.Keys);

                foreach (var resource in resources)
                {
                    var missing = union.Where(key => !resource.Keys.Contains(key))
                        .OrderBy(key => key, StringComparer.Ordinal)
                        .ToList();

                    if (missing.Count == 0)
                        continue;

                    var namespaceAttribute = resource.Root.Attribute("Namespace");

                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        namespaceAttribute is not null ? resource.CreateLocation(namespaceAttribute) : Location.None,
                        resource.Namespace,
                        resource.Culture,
                        missing.Count,
                        Summarise(missing)));
                }
            }
        }

        /// <summary>
        /// Formats the missing keys, naming a few and counting the remainder.
        /// </summary>
        /// <param name="missing">The missing keys, ordered.</param>
        /// <returns>A short human-readable summary.</returns>
        /// <remarks>
        /// A resource that is missing dozens of keys would otherwise produce a diagnostic message too
        /// long to read in build output.
        /// </remarks>
        private static string Summarise(IReadOnlyList<string> missing)
        {
            var sample = string.Join(", ", missing.Take(SampleSize));
            return missing.Count <= SampleSize
                ? sample
                : $"{sample} and {missing.Count - SampleSize} more";
        }
    }
}
