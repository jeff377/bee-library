using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reports BEE4007: a writable property is excluded from some serialization formats but not others.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Verified against MessagePack 3.1.7 and System.Text.Json: a member carrying only
    /// <c>[IgnoreMember]</c> disappears from the MessagePack payload but still rides the JSON wire. For a
    /// writable property that asymmetry means one format restores the value and the other does not.
    /// </para>
    /// <para>
    /// IMPORTANT: Get-only properties are deliberately out of scope. A read-only discriminator that is
    /// ignored for one format and kept for another is a legitimate pattern in this framework —
    /// <c>FilterNode.Kind</c> is ignored by MessagePack (the union tag already identifies the type) but
    /// must stay on the JSON wire, where it is the polymorphic discriminator. Reporting those would make
    /// the rule fire on correct code.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class IgnoreAttributeConsistencyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.InconsistentIgnoreAttributes,
            title: "Writable property should be ignored consistently across serialization formats",
            messageFormat: "Writable property '{1}' on '{0}' is excluded from {2} but not from {3}, so one "
                         + "format restores the value and the other does not. Fix: apply the missing "
                         + "attribute(s) — the framework convention is [XmlIgnore, JsonIgnore, "
                         + "IgnoreMember] together.",
            category: "Bee.Serialization",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A property excluded from only some formats is persisted or transmitted "
                       + "inconsistently depending on which serializer runs.",
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
                var messagePack = startContext.Compilation.GetTypeByMetadataName(
                    SerializationAttributeNames.IgnoreMemberAttribute);
                var json = startContext.Compilation.GetTypeByMetadataName(
                    SerializationAttributeNames.JsonIgnoreAttribute);
                var xml = startContext.Compilation.GetTypeByMetadataName(
                    SerializationAttributeNames.XmlIgnoreAttribute);

                // All three must be resolvable: with only some of them present, an "absent" attribute
                // cannot be distinguished from one the project has no reference for.
                if (messagePack is null || json is null || xml is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeProperty(symbolContext, messagePack, json, xml),
                    SymbolKind.Property);
            });
        }

        private static void AnalyzeProperty(
            SymbolAnalysisContext context,
            INamedTypeSymbol messagePack,
            INamedTypeSymbol json,
            INamedTypeSymbol xml)
        {
            var property = (IPropertySymbol)context.Symbol;

            // A publicly writable property is the only shape where the asymmetry has consequences.
            // Get-only and private-setter properties are skipped: XmlSerializer requires a public setter
            // to restore a value at all, so for those the difference between formats cannot change what
            // is read back. The framework's own IObjectSerialize.SerializeState members are exactly this
            // shape and are correct as written.
            if (property.DeclaredAccessibility != Accessibility.Public ||
                property.IsStatic ||
                property.SetMethod is null ||
                property.SetMethod.DeclaredAccessibility != Accessibility.Public)
            {
                return;
            }

            var attributes = property.GetAttributes();
            var ignoredBy = new List<string>();
            var keptBy = new List<string>();

            Classify(attributes, messagePack, "MessagePack", ignoredBy, keptBy);
            Classify(attributes, json, "JSON", ignoredBy, keptBy);
            Classify(attributes, xml, "XML", ignoredBy, keptBy);

            // Consistent either way: ignored everywhere, or serialized everywhere.
            if (ignoredBy.Count == 0 || keptBy.Count == 0)
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                property.Locations.FirstOrDefault() ?? Location.None,
                property.ContainingType?.Name ?? "(unknown)",
                property.Name,
                string.Join(" and ", ignoredBy),
                string.Join(" and ", keptBy)));
        }

        private static void Classify(
            ImmutableArray<AttributeData> attributes,
            INamedTypeSymbol ignoreAttribute,
            string formatName,
            List<string> ignoredBy,
            List<string> keptBy)
        {
            foreach (var attribute in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, ignoreAttribute))
                {
                    ignoredBy.Add(formatName);
                    return;
                }
            }

            keptBy.Add(formatName);
        }
    }
}
