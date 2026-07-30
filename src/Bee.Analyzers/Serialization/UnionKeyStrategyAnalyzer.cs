using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reports BEE4003: a type in a polymorphic union hierarchy opts into name-based MessagePack keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Union hierarchies keep integer <c>[Key]</c> numbering by framework decision (ADR-030), which makes
    /// them the one documented exception to "MessagePack is name-based everywhere". This rule holds that
    /// exception in place so the hierarchy stays on one keying strategy as it grows.
    /// </para>
    /// <para>
    /// NOTE: The basis is that decision, not a technical failure. MessagePack 3.1.7 was measured to
    /// round-trip name-based keys on a union base, on a subclass, and on both — including a hierarchy
    /// shaped like the framework's own <c>FilterNode</c> family. Anyone revisiting this rule should treat
    /// it as enforcing a convention, and should not expect a reproducible serialization bug behind it.
    /// The condition to relax it is a decision to change the convention, not new evidence about
    /// compatibility.
    /// </para>
    /// <para>
    /// Both halves of a hierarchy are covered: the base carrying <c>[Union]</c> and every type deriving
    /// from it. Derived types are found by walking the base chain, so no list of subtypes has to be
    /// maintained anywhere.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnionKeyStrategyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.UnionMustUseIntegerKeys,
            title: "Union hierarchy must use integer MessagePack keys",
            messageFormat: "'{0}' belongs to the polymorphic union hierarchy rooted at '{1}' but declares "
                         + "keyAsPropertyName. Union hierarchies keep integer [Key] numbering so the whole "
                         + "hierarchy shares one keying strategy. Fix: remove keyAsPropertyName and assign "
                         + "an explicit [Key(n)] to each member.",
            category: "Bee.Serialization",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Polymorphic union hierarchies are kept on integer keys by framework decision. "
                       + "Mixing keying strategies within one hierarchy splits its wire format.",
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
                var unionAttribute = startContext.Compilation.GetTypeByMetadataName(
                    SerializationAttributeNames.UnionAttribute);
                if (objectAttribute is null || unionAttribute is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeType(symbolContext, objectAttribute, unionAttribute),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(
            SymbolAnalysisContext context,
            INamedTypeSymbol objectAttribute,
            INamedTypeSymbol unionAttribute)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class)
                return;

            var contract = type.GetAttributes().FirstOrDefault(
                data => SymbolEqualityComparer.Default.Equals(data.AttributeClass, objectAttribute));
            if (contract is null || !MessagePackContract.UsesNameBasedKeys(contract))
                return;

            var root = FindUnionRoot(type, unionAttribute);
            if (root is null)
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                type.Locations.FirstOrDefault() ?? Location.None,
                type.Name,
                root.Name));
        }

        /// <summary>
        /// Finds the type declaring <c>[Union]</c> at the root of the specified type's hierarchy.
        /// </summary>
        /// <param name="type">The type to start from, inclusive.</param>
        /// <param name="unionAttribute">The resolved <c>Union</c> attribute symbol.</param>
        /// <returns>
        /// The union root, or <c>null</c> when the type is not part of a union hierarchy.
        /// </returns>
        private static INamedTypeSymbol? FindUnionRoot(INamedTypeSymbol type, INamedTypeSymbol unionAttribute)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.GetAttributes()
                    .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, unionAttribute)))
                {
                    return current;
                }
            }

            return null;
        }
    }
}
