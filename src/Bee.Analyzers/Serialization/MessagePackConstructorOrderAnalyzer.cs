using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Serialization
{
    /// <summary>
    /// Reports BEE4004: a constructor of a type using integer MessagePack keys takes its parameters in
    /// an order that disagrees with the key order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MessagePack selects the public constructor with the most parameters and fills those parameters
    /// <b>in key order</b>, by position — it does not match parameter names to members. A constructor
    /// whose parameter order disagrees with the key order therefore assigns each value to the wrong
    /// member, silently, with no exception and no data loss that a length check would catch.
    /// </para>
    /// <para>
    /// Verified against MessagePack 3.1.7: the corruption happens even when the type also declares a
    /// parameterless constructor (it is not preferred) and even when every parameter name matches its
    /// member name exactly. XML and JSON round-trips are unaffected, so tests in those formats pass
    /// while the wire silently swaps fields.
    /// </para>
    /// <para>
    /// Types using <c>keyAsPropertyName: true</c> are out of scope: name-based keys are matched by
    /// name, and a reversed constructor round-trips correctly. In practice this rule therefore applies
    /// to the <c>[Union]</c> polymorphic families, which cannot use name-based keys.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MessagePackConstructorOrderAnalyzer : DiagnosticAnalyzer
    {
        private const string MessagePackObjectAttribute = "MessagePack.MessagePackObjectAttribute";
        private const string KeyAttribute = "MessagePack.KeyAttribute";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.ConstructorParameterOrderMismatch,
            title: "MessagePack constructor parameters must follow integer key order",
            messageFormat: "Constructor of '{0}' takes parameters in the order ({1}), which does not follow "
                         + "the MessagePack key order ({2}). MessagePack fills constructor parameters by "
                         + "position in key order, so values are silently assigned to the wrong members on "
                         + "the wire while XML and JSON round-trips still pass. Fix: reorder the parameters "
                         + "to ({2}).",
            category: "Bee.Serialization",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Types with integer MessagePack keys have their constructor parameters filled by "
                       + "position in key order. A constructor that departs from that order corrupts the "
                       + "wire format without raising an error.");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(startContext =>
            {
                // Resolved by metadata name because the analyzer targets netstandard2.0 and cannot
                // reference MessagePack. A consumer that does not use MessagePack resolves to null,
                // which silences the rule instead of reporting against unrelated code.
                var objectAttribute = startContext.Compilation.GetTypeByMetadataName(MessagePackObjectAttribute);
                var keyAttribute = startContext.Compilation.GetTypeByMetadataName(KeyAttribute);
                if (objectAttribute is null || keyAttribute is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeType(symbolContext, objectAttribute, keyAttribute),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(
            SymbolAnalysisContext context,
            INamedTypeSymbol objectAttribute,
            INamedTypeSymbol keyAttribute)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class)
                return;

            var attribute = type.GetAttributes().FirstOrDefault(
                data => SymbolEqualityComparer.Default.Equals(data.AttributeClass, objectAttribute));
            if (attribute is null || UsesNameBasedKeys(attribute))
                return;

            var keysByMember = CollectIntegerKeys(type, keyAttribute);
            if (keysByMember.Count == 0)
                return;

            foreach (var constructor in type.InstanceConstructors)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (constructor.DeclaredAccessibility != Accessibility.Public || constructor.Parameters.Length < 2)
                    continue;

                var keys = MapParametersToKeys(constructor, keysByMember);

                // A parameter that matches no keyed member means this constructor is not the one
                // MessagePack reconstructs from, so its order carries no wire meaning.
                if (keys is null || IsAscending(keys))
                    continue;

                var actualOrder = string.Join(", ", constructor.Parameters.Select(parameter => parameter.Name));
                var expectedOrder = string.Join(", ", constructor.Parameters
                    .Zip(keys, (parameter, key) => (parameter.Name, Key: key))
                    .OrderBy(pair => pair.Key)
                    .Select(pair => pair.Name));

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    constructor.Locations.FirstOrDefault() ?? Location.None,
                    type.Name,
                    actualOrder,
                    expectedOrder));
            }
        }

        /// <summary>
        /// Determines whether the attribute opts the type into name-based keys.
        /// </summary>
        /// <param name="attribute">The <c>MessagePackObject</c> attribute applied to the type.</param>
        /// <returns><c>true</c> when <c>keyAsPropertyName</c> is set.</returns>
        private static bool UsesNameBasedKeys(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is bool positional)
                return positional;

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == "keyAsPropertyName" && argument.Value.Value is bool named)
                    return named;
            }

            return false;
        }

        /// <summary>
        /// Collects the integer keys declared on the type and its base types.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="keyAttribute">The resolved <c>Key</c> attribute symbol.</param>
        /// <returns>A map of member name to key, comparing names case-insensitively.</returns>
        /// <remarks>
        /// Base types are included because a keyed member can be inherited, and the constructor of the
        /// derived type may well take it as a parameter.
        /// </remarks>
        private static Dictionary<string, int> CollectIntegerKeys(INamedTypeSymbol type, INamedTypeSymbol keyAttribute)
        {
            var keys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var current = type; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is not IPropertySymbol && member is not IFieldSymbol)
                        continue;

                    var keyData = member.GetAttributes().FirstOrDefault(
                        data => SymbolEqualityComparer.Default.Equals(data.AttributeClass, keyAttribute));
                    if (keyData is null || keyData.ConstructorArguments.Length != 1)
                        continue;

                    if (keyData.ConstructorArguments[0].Value is int key && !keys.ContainsKey(member.Name))
                        keys[member.Name] = key;
                }
            }

            return keys;
        }

        /// <summary>
        /// Maps each constructor parameter onto the key of the member it corresponds to.
        /// </summary>
        /// <param name="constructor">The constructor to map.</param>
        /// <param name="keysByMember">The keys declared on the type.</param>
        /// <returns>
        /// The keys in parameter order, or <c>null</c> when any parameter matches no keyed member.
        /// </returns>
        private static List<int>? MapParametersToKeys(
            IMethodSymbol constructor,
            Dictionary<string, int> keysByMember)
        {
            var keys = new List<int>(constructor.Parameters.Length);

            foreach (var parameter in constructor.Parameters)
            {
                if (!keysByMember.TryGetValue(parameter.Name, out var key))
                    return null;

                keys.Add(key);
            }

            return keys;
        }

        /// <summary>
        /// Determines whether the keys are in ascending order.
        /// </summary>
        /// <param name="keys">The keys in parameter order.</param>
        /// <returns><c>true</c> when each key is greater than the one before it.</returns>
        private static bool IsAscending(List<int> keys)
        {
            for (var index = 1; index < keys.Count; index++)
            {
                if (keys[index] < keys[index - 1])
                    return false;
            }

            return true;
        }
    }
}
