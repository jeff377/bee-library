using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reports BEE4006: a serialized type has no public parameterless constructor.
    /// </summary>
    /// <remarks>
    /// Verified with <c>XmlSerializer</c>: deserializing a type without a parameterless constructor
    /// throws <c>MissingMethodException</c> ("Cannot dynamically create an instance ... No parameterless
    /// constructor defined"). This applies on every platform, not only AOT targets — the framework's
    /// definition files are XML, so any type reachable from a definition or a persisted payload needs
    /// one. The failure happens at run time when the file is first read.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParameterlessConstructorAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.MissingParameterlessConstructor,
            title: "Serialized type must have a public parameterless constructor",
            messageFormat: "'{0}' takes part in serialization but declares no public parameterless "
                         + "constructor, so deserialization throws MissingMethodException when the type "
                         + "is first read back. Fix: add a public parameterless constructor alongside the "
                         + "existing one.",
            category: "Bee.Serialization",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Deserializers construct instances before populating them, which requires a "
                       + "public parameterless constructor. Declaring only a parameterised constructor "
                       + "makes the type impossible to read back.",
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
                var collections = FrameworkCollectionTypes.TryResolve(startContext.Compilation);
                if (objectAttribute is null && collections is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeType(symbolContext, objectAttribute, collections),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(
            SymbolAnalysisContext context,
            INamedTypeSymbol? objectAttribute,
            FrameworkCollectionTypes? collections)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            // Abstract types are never constructed by a deserializer; the concrete subclass is what
            // needs the constructor, and it is analyzed separately.
            if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic)
                return;

            if (!ParticipatesInSerialization(type, objectAttribute, collections))
                return;

            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public)
                    return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                type.Locations.FirstOrDefault() ?? Location.None,
                type.Name));
        }

        /// <summary>
        /// Determines whether the type is one a deserializer will need to construct.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <param name="objectAttribute">The resolved <c>MessagePackObject</c> attribute, if available.</param>
        /// <param name="collections">The resolved framework collection bases, if available.</param>
        /// <returns><c>true</c> when the type carries a serialization contract or is a collection.</returns>
        private static bool ParticipatesInSerialization(
            INamedTypeSymbol type,
            INamedTypeSymbol? objectAttribute,
            FrameworkCollectionTypes? collections)
        {
            if (collections is not null && collections.IsFrameworkCollection(type))
                return true;

            if (objectAttribute is null)
                return false;

            foreach (var attribute in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, objectAttribute))
                    return true;
            }

            return false;
        }
    }
}
