using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Conventions
{
    /// <summary>
    /// Reports BEE3003: a public method on an ExecFunc handler that no <c>[ExecFuncAccessControl]</c> covers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ExecFunc is a second dispatch surface alongside ordinary business object actions: a client sends a
    /// FuncID and the framework resolves it straight to a public method on the handler, by name. BEE3001
    /// cannot see these methods because they live on <c>IExecFuncHandler</c> implementations rather than
    /// on a business object, which left this surface with no build-time gate at all.
    /// </para>
    /// <para>
    /// Dispatch is fail-closed, so an unmarked method is rejected at run time rather than silently
    /// exposed. This rule exists so the omission surfaces during the build instead of on a client's
    /// first call — and, more to the point, so that adding a maintenance operation to a handler forces
    /// a deliberate decision about who may reach it.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExecFuncAccessControlAnalyzer : DiagnosticAnalyzer
    {
        private const string HandlerInterface = "Bee.Business.IExecFuncHandler";
        private const string AccessControlAttribute = "Bee.Business.Attributes.ExecFuncAccessControlAttribute";
        private const string ArgsType = "Bee.Business.ExecFuncArgs";
        private const string ResultType = "Bee.Business.ExecFuncResult";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.MissingExecFuncAccessControl,
            title: "ExecFunc method must declare access control",
            messageFormat: "Public method '{1}' on ExecFunc handler '{0}' has no [ExecFuncAccessControl]. "
                         + "Dispatch is fail-closed, so the method is rejected at run time and cannot be "
                         + "called at all. Fix: add [ExecFuncAccessControl(...)], and set LocalOnly = true "
                         + "when the operation acts on a caller-supplied database target.",
            category: "Bee.Business",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Public methods on an ExecFunc handler are reachable as FuncIDs and require an "
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
                var handlerInterface = startContext.Compilation.GetTypeByMetadataName(HandlerInterface);
                var accessControl = startContext.Compilation.GetTypeByMetadataName(AccessControlAttribute);
                var argsType = startContext.Compilation.GetTypeByMetadataName(ArgsType);
                var resultType = startContext.Compilation.GetTypeByMetadataName(ResultType);
                if (handlerInterface is null || accessControl is null || argsType is null || resultType is null)
                    return;

                startContext.RegisterSymbolAction(
                    symbolContext => AnalyzeType(symbolContext, handlerInterface, accessControl, argsType, resultType),
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(
            SymbolAnalysisContext context,
            INamedTypeSymbol handlerInterface,
            INamedTypeSymbol accessControl,
            INamedTypeSymbol argsType,
            INamedTypeSymbol resultType)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class || !ImplementsHandler(type, handlerInterface))
                return;

            foreach (var member in type.GetMembers())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (member is not IMethodSymbol method || !IsFuncCandidate(method, argsType, resultType))
                    continue;

                if (HasAttribute(method, accessControl))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    type.Name,
                    method.Name));
            }
        }

        /// <summary>
        /// Determines whether the method matches the signature the ExecFunc dispatcher can invoke.
        /// </summary>
        /// <param name="method">The method to test.</param>
        /// <param name="argsType">The resolved <c>ExecFuncArgs</c> symbol.</param>
        /// <param name="resultType">The resolved <c>ExecFuncResult</c> symbol.</param>
        /// <returns><c>true</c> when the method forms part of the ExecFunc surface.</returns>
        /// <remarks>
        /// The dispatcher resolves a FuncID by name and invokes with exactly two arguments, so only
        /// methods of that shape are reachable. Matching the signature — rather than every public
        /// method — keeps helpers and properties on a handler out of the rule.
        /// </remarks>
        private static bool IsFuncCandidate(IMethodSymbol method, INamedTypeSymbol argsType, INamedTypeSymbol resultType)
        {
            if (method.MethodKind != MethodKind.Ordinary || method.DeclaredAccessibility != Accessibility.Public)
                return false;

            if (method.Parameters.Length != 2)
                return false;

            return SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, argsType)
                && SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, resultType);
        }

        private static bool ImplementsHandler(INamedTypeSymbol type, INamedTypeSymbol handlerInterface)
        {
            return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, handlerInterface));
        }

        private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
        {
            return symbol.GetAttributes()
                .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
        }
    }
}
