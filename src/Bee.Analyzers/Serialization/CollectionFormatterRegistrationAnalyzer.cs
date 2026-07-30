using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reports BEE4001: a collection deriving from <c>MessagePackCollectionBase</c> that no
    /// <c>CollectionBaseFormatter</c> registers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registration list is written by hand, and nothing connects it to the collections it is meant
    /// to cover: adding a collection without adding its formatter compiles cleanly and passes every test
    /// that does not exercise the wire.
    /// </para>
    /// <para>
    /// Verified against MessagePack 3.1.7 with the framework's resolver order. The observable failure is
    /// <b>not</b> a silently empty collection: serialization writes the elements correctly, and it is
    /// <b>deserialization</b> that throws <c>MessagePackSerializationException</c>. The failure therefore
    /// appears only when a payload is read back, which can be a different process from the one that wrote
    /// it.
    /// </para>
    /// <para>
    /// IMPORTANT: The rule only runs in the compilation that owns the registration list, detected by
    /// finding <c>CollectionBaseFormatter</c> constructions. That type is internal to the assembly
    /// holding the list, so every other project resolves it to null and the rule stays silent — which is
    /// what stops it reporting every collection in the projects that merely define them.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CollectionFormatterRegistrationAnalyzer : DiagnosticAnalyzer
    {
        private const string CollectionBaseFormatter = "Bee.Api.Core.MessagePack.CollectionBaseFormatter`2";
        private const string FrameworkAssemblyPrefix = "Bee.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.CollectionFormatterNotRegistered,
            title: "MessagePack collection must be registered with a formatter",
            messageFormat: "Collection '{0}' derives from MessagePackCollectionBase but no "
                         + "CollectionBaseFormatter registers it. Serialization writes its elements "
                         + "correctly and only deserialization throws MessagePackSerializationException, "
                         + "so the failure surfaces when a payload is read back rather than when it is "
                         + "written. Fix: add new CollectionBaseFormatter<{0}, {1}>() to the formatter "
                         + "array.",
            category: "Bee.Serialization",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Collections are matched to formatters by a hand-written registration list. "
                       + "A collection missing from it cannot be deserialized.",
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
                var formatter = startContext.Compilation.GetTypeByMetadataName(CollectionBaseFormatter);
                var collectionBase = startContext.Compilation.GetTypeByMetadataName(
                    SerializationAttributeNames.MessagePackCollectionBase);
                if (formatter is null || collectionBase is null)
                    return;

                // Collected across the whole compilation and compared at the end. Registrations are
                // gathered through an operation action rather than by asking the compilation for a
                // semantic model, which an analyzer must not do (RS1030).
                var registered = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
                var anchors = new ConcurrentBag<Location>();

                startContext.RegisterOperationAction(
                    operationContext => CollectRegistration(operationContext, formatter, registered, anchors),
                    OperationKind.ObjectCreation);

                startContext.RegisterCompilationEndAction(
                    endContext => Report(endContext, collectionBase, registered, anchors));
            });
        }

        private static void CollectRegistration(
            OperationAnalysisContext context,
            INamedTypeSymbol formatter,
            ConcurrentDictionary<string, byte> registered,
            ConcurrentBag<Location> anchors)
        {
            if (context.Operation.Type is not INamedTypeSymbol constructed ||
                !constructed.IsGenericType ||
                !SymbolEqualityComparer.Default.Equals(constructed.OriginalDefinition, formatter) ||
                constructed.TypeArguments.Length == 0)
            {
                return;
            }

            if (constructed.TypeArguments[0] is INamedTypeSymbol collection)
            {
                registered.TryAdd(collection.ToDisplayString(), 0);
                anchors.Add(context.Operation.Syntax.GetLocation());
            }
        }

        private static void Report(
            CompilationAnalysisContext context,
            INamedTypeSymbol collectionBase,
            ConcurrentDictionary<string, byte> registered,
            ConcurrentBag<Location> anchors)
        {
            // An empty list means this compilation does not own the registrations, so it has no basis
            // for deciding which collections are missing.
            if (registered.IsEmpty)
                return;

            var anchor = anchors.FirstOrDefault() ?? Location.None;

            foreach (var collection in EnumerateCollections(context, collectionBase))
            {
                if (registered.ContainsKey(collection.ToDisplayString()))
                    continue;

                var elementType = FindElementType(collection, collectionBase);

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    anchor,
                    collection.Name,
                    elementType?.Name ?? "TElement"));
            }
        }

        /// <summary>
        /// Enumerates every concrete collection deriving from the framework collection base.
        /// </summary>
        /// <param name="context">The compilation analysis context.</param>
        /// <param name="collectionBase">The resolved collection base definition.</param>
        /// <returns>The candidate collection types.</returns>
        /// <remarks>
        /// Referenced assemblies have to be walked, not just source: the registration list lives in one
        /// assembly while the collections themselves are declared in another. The walk is limited to
        /// framework assemblies so that the base class library is never enumerated.
        /// </remarks>
        private static IEnumerable<INamedTypeSymbol> EnumerateCollections(
            CompilationAnalysisContext context,
            INamedTypeSymbol collectionBase)
        {
            var sourceTypes = EnumerateTypes(context.Compilation.Assembly.GlobalNamespace, context.CancellationToken);
            foreach (var type in sourceTypes.Where(type => IsCandidate(type, collectionBase)))
            {
                yield return type;
            }

            foreach (var reference in context.Compilation.References)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (context.Compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly ||
                    !assembly.Name.StartsWith(FrameworkAssemblyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var referencedTypes = EnumerateTypes(assembly.GlobalNamespace, context.CancellationToken);
                foreach (var type in referencedTypes.Where(type => IsCandidate(type, collectionBase)))
                {
                    yield return type;
                }
            }
        }

        private static bool IsCandidate(INamedTypeSymbol type, INamedTypeSymbol collectionBase)
            => type.TypeKind == TypeKind.Class
            && !type.IsAbstract
            && !type.IsGenericType
            && FindElementType(type, collectionBase) is not null;

        /// <summary>
        /// Gets the element type the collection is closed over, if it derives from the collection base.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="collectionBase">The resolved collection base definition.</param>
        /// <returns>The element type, or <c>null</c> when the type is not such a collection.</returns>
        private static ITypeSymbol? FindElementType(INamedTypeSymbol type, INamedTypeSymbol collectionBase)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType &&
                    SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, collectionBase) &&
                    current.TypeArguments.Length == 1)
                {
                    return current.TypeArguments[0];
                }
            }

            return null;
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(
            INamespaceSymbol root,
            CancellationToken cancellationToken)
        {
            foreach (var member in root.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (member)
                {
                    case INamespaceSymbol nested:
                        foreach (var type in EnumerateTypes(nested, cancellationToken))
                            yield return type;
                        break;

                    case INamedTypeSymbol type:
                        yield return type;
                        break;
                }
            }
        }
    }
}
