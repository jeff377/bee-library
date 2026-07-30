using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Conventions
{
    /// <summary>
    /// Reports BEE3001: a public method on a business object that no <c>[ApiAccessControl]</c> covers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A business object's public methods are its API surface: the JSON-RPC executor resolves an action
    /// name straight to a public method. Before invoking it the framework demands an access-control
    /// declaration and throws <c>UnauthorizedAccessException</c> when none is found, so an unmarked
    /// method is not an open one — it is one that cannot be called at all, and only says so at run time
    /// once a client tries.
    /// </para>
    /// <para>
    /// The attribute is looked up exactly as the framework does: on the method itself, then on the method
    /// it overrides, then on the declaring type. A type-level attribute therefore covers all of its
    /// methods, and only genuinely uncovered methods are reported.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BusinessObjectAccessControlAnalyzer : DiagnosticAnalyzer
    {
        private const string BusinessObjectType = "Bee.Business.BusinessObject";
        private const string AccessControlAttribute = "Bee.Definition.Attributes.ApiAccessControlAttribute";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.MissingApiAccessControl,
            title: "Business object API method must declare access control",
            messageFormat: "Public method '{1}' on business object '{0}' has no [ApiAccessControl] on "
                         + "itself, on the method it overrides, or on its declaring type. The framework "
                         + "rejects such calls with UnauthorizedAccessException, so the method cannot be "
                         + "invoked at all — the failure only appears when a client first calls it. "
                         + "Fix: add [ApiAccessControl] to the method, or to the type to cover all of "
                         + "its methods.",
            category: "Bee.Business",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Public methods on a business object are reachable as API actions and require an "
                       + "access-control declaration before the framework will invoke them.",
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
                var businessObject = startContext.Compilation.GetTypeByMetadataName(BusinessObjectType);
                var accessControl = startContext.Compilation.GetTypeByMetadataName(AccessControlAttribute);
                if (businessObject is null || accessControl is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeType(symbolContext, businessObject, accessControl),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(
            SymbolAnalysisContext context,
            INamedTypeSymbol businessObject,
            INamedTypeSymbol accessControl)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class || !DerivesFrom(type, businessObject))
                return;

            // A type-level attribute covers every method, so there is nothing left to report.
            if (HasAttribute(type, accessControl))
                return;

            foreach (var member in type.GetMembers())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (member is not IMethodSymbol method || !IsApiCandidate(method))
                    continue;

                if (HasAttribute(method, accessControl) || IsCoveredByOverriddenMethod(method, accessControl))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    type.Name,
                    method.Name));
            }
        }

        /// <summary>
        /// Determines whether the method is one the JSON-RPC executor could resolve as an action.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <returns><c>true</c> when the method forms part of the API surface.</returns>
        /// <remarks>
        /// Constructors and accessors are excluded because an action name never resolves to them. The
        /// framework's own business objects mark every remaining public method, so this filter is what
        /// keeps the rule at a near-zero false positive rate.
        /// </remarks>
        private static bool IsApiCandidate(IMethodSymbol method)
            => method.MethodKind == MethodKind.Ordinary
            && !method.IsStatic
            && method.DeclaredAccessibility == Accessibility.Public;

        /// <summary>
        /// Determines whether an overridden method further up the hierarchy carries the attribute.
        /// </summary>
        /// <param name="method">The overriding method.</param>
        /// <param name="accessControl">The resolved attribute symbol.</param>
        /// <returns><c>true</c> when any overridden method declares access control.</returns>
        private static bool IsCoveredByOverriddenMethod(IMethodSymbol method, INamedTypeSymbol accessControl)
        {
            for (var current = method.OverriddenMethod; current is not null; current = current.OverriddenMethod)
            {
                if (HasAttribute(current, accessControl) ||
                    (current.ContainingType is not null && HasAttribute(current.ContainingType, accessControl)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                    return true;
            }

            return false;
        }

        private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;
            }

            return false;
        }
    }
}
