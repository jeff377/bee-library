using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reports BEE4005: a framework collection declares an extra public <c>Add</c> overload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reflection-only XML serialization resolves a collection's add method with a single
    /// <c>Type.GetMethod("Add")</c> call, which throws <c>AmbiguousMatchException</c> when more than one
    /// public instance overload exists. The framework bases avoid this deliberately: they expose exactly
    /// one public <c>Add(T)</c> and route the interface variants through explicit implementations, so any
    /// additional public overload has to come from a derived type.
    /// </para>
    /// <para>
    /// IMPORTANT: This is a warning rather than an error because the failure could not be reproduced on
    /// the desktop — not even with <c>IsDynamicCodeSupported</c> forced to false, which normally
    /// reproduces the iOS AOT serialization path. The rule rests on a defect that was diagnosed and
    /// fixed previously rather than on a case that reproduces today, so it does not get error severity
    /// until an iOS device run confirms it.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CollectionAddOverloadAnalyzer : DiagnosticAnalyzer
    {
        private const string AddMethodName = "Add";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.AmbiguousCollectionAdd,
            title: "Framework collection should expose a single public Add",
            messageFormat: "Collection '{0}' declares an additional public Add overload. Reflection-only "
                         + "XML serialization resolves the add method with a single GetMethod(\"Add\") "
                         + "call and throws AmbiguousMatchException when several public overloads exist, "
                         + "which affects mobile and AOT targets but never the desktop. "
                         + "Fix: move the convenience overload to an extension method.",
            category: "Bee.Serialization",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The framework collection bases expose exactly one public Add so that "
                       + "reflection-only serialization can resolve it. Additional overloads reintroduce "
                       + "the ambiguity on platforms without dynamic code generation.",
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
                var collections = FrameworkCollectionTypes.TryResolve(startContext.Compilation);
                if (collections is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeType(symbolContext, collections),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(SymbolAnalysisContext context, FrameworkCollectionTypes collections)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class || !collections.IsFrameworkCollection(type))
                return;

            // Only overloads declared on the derived type are reported: the base already contributes the
            // one permitted public Add, so anything declared here is what pushes the count past one.
            foreach (var member in type.GetMembers(AddMethodName))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (member is not IMethodSymbol method ||
                    method.IsStatic ||
                    method.MethodKind != MethodKind.Ordinary ||
                    method.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    type.Name));
            }
        }
    }
}
