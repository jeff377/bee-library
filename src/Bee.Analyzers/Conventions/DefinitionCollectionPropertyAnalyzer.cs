using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Bee.Analyzers.Serialization;

namespace Bee.Analyzers.Conventions
{
    /// <summary>
    /// Reports BEE3002: a framework type exposes a collection property typed as a plain
    /// <c>List&lt;T&gt;</c> or <c>Collection&lt;T&gt;</c> instead of a framework collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Framework collections carry more than element storage: an owner back-reference, serialization
    /// state that the framework's own persistence flow reads, keyed lookup, and the
    /// <c>CollectionBaseFormatter</c> wire path. A plain collection serializes without error but silently
    /// has none of that, so the loss shows up as behaviour that is missing rather than as a failure.
    /// </para>
    /// <para>
    /// IMPORTANT: This rule only runs inside the <c>Bee.Definition</c> assembly. "Definition type" is not
    /// something an analyzer can identify reliably — an ordinary DTO with a <c>List&lt;string&gt;</c>
    /// property looks exactly the same — and the convention was only ever about the definition layer.
    /// Widening it to all framework assemblies was measured and rejected: it reported three cross-layer
    /// DTOs in <c>Bee.Business</c> (<c>Queries</c> and friends) where a plain list
    /// is correct, because those types need none of the behaviour above. Unlike the other rules here,
    /// BEE3002 therefore serves the framework's own consistency rather than the consumer's build.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DefinitionCollectionPropertyAnalyzer : DiagnosticAnalyzer
    {
        private const string DefinitionAssemblyName = "Bee.Definition";
        private const string ListType = "System.Collections.Generic.List`1";
        private const string CollectionType = "System.Collections.ObjectModel.Collection`1";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.NonFrameworkCollectionProperty,
            title: "Framework collection property must use a framework collection type",
            messageFormat: "Property '{1}' on '{0}' is typed as '{2}', which carries no owner "
                         + "back-reference, no serialization state and no keyed lookup, and does not take "
                         + "the CollectionBaseFormatter wire path. It serializes without error, so the "
                         + "missing behaviour surfaces as absent features rather than a failure. "
                         + "Fix: declare a collection deriving from KeyCollectionBase or CollectionBase.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Framework collections provide owner tracking, serialization state and keyed "
                       + "lookup that plain collections lack.",
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(startContext =>
            {
                // Consumer projects are out of scope: see the remarks on this type.
                if (!string.Equals(
                    startContext.Compilation.Assembly.Name, DefinitionAssemblyName, StringComparison.Ordinal))
                {
                    return;
                }

                var list = startContext.Compilation.GetTypeByMetadataName(ListType);
                var collection = startContext.Compilation.GetTypeByMetadataName(CollectionType);
                var frameworkCollections = FrameworkCollectionTypes.TryResolve(startContext.Compilation);
                if ((list is null && collection is null) || frameworkCollections is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeProperty(symbolContext, list, collection, frameworkCollections),
                    SymbolKind.Property);
            });
        }

        private static void AnalyzeProperty(
            SymbolAnalysisContext context,
            INamedTypeSymbol? list,
            INamedTypeSymbol? collection,
            FrameworkCollectionTypes frameworkCollections)
        {
            var property = (IPropertySymbol)context.Symbol;

            // The collection bases themselves are out of scope: their own serialization plumbing (the
            // flattening proxy property, for instance) deliberately uses a plain list, because that is
            // exactly the shape it puts on the wire.
            if (property.ContainingType is not null && frameworkCollections.IsCollectionBase(property.ContainingType))
                return;

            if (property.DeclaredAccessibility != Accessibility.Public ||
                property.IsStatic ||
                property.Type is not INamedTypeSymbol propertyType ||
                !propertyType.IsGenericType)
            {
                return;
            }

            var definition = propertyType.OriginalDefinition;
            var isPlainCollection =
                (list is not null && SymbolEqualityComparer.Default.Equals(definition, list)) ||
                (collection is not null && SymbolEqualityComparer.Default.Equals(definition, collection));

            if (!isPlainCollection)
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                property.Locations.FirstOrDefault() ?? Location.None,
                property.ContainingType?.Name ?? "(unknown)",
                property.Name,
                propertyType.Name));
        }
    }
}
