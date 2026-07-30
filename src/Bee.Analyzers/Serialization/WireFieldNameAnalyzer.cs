using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reports BEE4002: a name-based MessagePack type renames a property for JSON only, so the two wire
    /// formats disagree on the field name.
    /// </summary>
    /// <remarks>
    /// Verified against MessagePack 3.1.7 and System.Text.Json: a property named <c>Name</c> carrying
    /// <c>[JsonPropertyName("alias_name")]</c> on a <c>keyAsPropertyName</c> type serialises as
    /// <c>{"alias_name":"value"}</c> in JSON but <c>{"Name":"value"}</c> in MessagePack. Both formats
    /// round-trip against themselves, so tests pass; the mismatch only surfaces when a client speaking
    /// one format talks to a server configured for the other.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WireFieldNameAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.WireFieldNameMismatch,
            title: "JSON property rename conflicts with name-based MessagePack keys",
            messageFormat: "Property '{1}' on '{0}' is renamed to '{2}' for JSON, but the type uses "
                         + "keyAsPropertyName so MessagePack keys it as '{1}'. The two wire formats "
                         + "disagree on the field name and each still round-trips against itself, so "
                         + "tests will not catch it. Fix: remove the JsonPropertyName rename, or rename "
                         + "the property itself so both formats agree.",
            category: "Bee.Serialization",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Name-based MessagePack keys are the property names. Renaming a property for "
                       + "JSON alone makes the payload field names depend on which format is in use.",
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
                var objectAttribute = startContext.Compilation.GetTypeByMetadataName(
                    SerializationAttributeNames.MessagePackObjectAttribute);
                var jsonPropertyName = startContext.Compilation.GetTypeByMetadataName(
                    SerializationAttributeNames.JsonPropertyNameAttribute);
                if (objectAttribute is null || jsonPropertyName is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeType(symbolContext, objectAttribute, jsonPropertyName),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(
            SymbolAnalysisContext context,
            INamedTypeSymbol objectAttribute,
            INamedTypeSymbol jsonPropertyName)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
                return;

            var contract = type.GetAttributes().FirstOrDefault(
                data => SymbolEqualityComparer.Default.Equals(data.AttributeClass, objectAttribute));

            // Integer-keyed types are unaffected: their MessagePack keys are numbers, so a JSON rename
            // cannot disagree with them.
            if (contract is null || !MessagePackContract.UsesNameBasedKeys(contract))
                return;

            foreach (var member in type.GetMembers())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (member is not IPropertySymbol property || property.DeclaredAccessibility != Accessibility.Public)
                    continue;

                var rename = property.GetAttributes().FirstOrDefault(
                    data => SymbolEqualityComparer.Default.Equals(data.AttributeClass, jsonPropertyName));
                if (rename is null || rename.ConstructorArguments.Length == 0)
                    continue;

                if (rename.ConstructorArguments[0].Value is not string alias ||
                    string.Equals(alias, property.Name, StringComparison.Ordinal))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    property.Locations.FirstOrDefault() ?? Location.None,
                    type.Name,
                    property.Name,
                    alias));
            }
        }
    }
}
