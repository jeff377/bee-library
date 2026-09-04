using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// Reports BEE1008: a form schema declares no <c>PermissionModelId</c>, so nothing authorizes it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Layer-1 authorization reads the form's <c>PermissionModelId</c> and returns immediately when
    /// it is empty. That is deliberate — the framework's XML doc calls it gradual adoption, and
    /// unmarked forms stay open — but the consequence has never been visible anywhere: a form that
    /// should have been marked and was not is open to every authenticated caller, and the build,
    /// the tests and the existing analyzers all stay green.
    /// </para>
    /// <para>
    /// WARNING: this reports, it does not enforce. Changing the runtime to refuse unmarked forms
    /// would break every deployment mid-adoption, which is exactly what the opt-in exists to avoid.
    /// The point is that "this form is open" becomes something you are told rather than something
    /// you have to notice.
    /// </para>
    /// <para>
    /// Severity is <see cref="DiagnosticSeverity.Info"/> for the same reason. A warning would fail
    /// the build of every consumer that has not finished adopting permission models, and adoption is
    /// the thing the opt-in exists to allow. (The framework's own <c>Defaults/</c> would qualify —
    /// <c>Department</c> and <c>Employee</c> carry no model — though those files are not analyzed
    /// here: the definition rules read a consumer's <c>Define/**</c> glob.)
    /// A deployment that has finished adopting them should raise it in <c>.editorconfig</c>:
    /// <code>dotnet_diagnostic.BEE1008.severity = warning</code>
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PermissionModelAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.MissingPermissionModelId,
            title: "FormSchema declares no permission model",
            messageFormat: "FormSchema '{0}' declares no PermissionModelId, so every authenticated caller may read and write through it. "
                         + "Declare one to bring the form under permission control, or leave it open deliberately.",
            category: "Bee.Definition",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Layer-1 authorization is a no-op for a form with no PermissionModelId. That is the "
                       + "framework's gradual-adoption default, so this reports rather than enforces — but an "
                       + "unmarked form is open to every authenticated caller, and nothing else says so. "
                       + "Raise the severity to warning once a deployment has finished adopting permission models.",
            helpLinkUri: null,
            // NOTE: Definition files are only reachable from a compilation action, which makes every
            // diagnostic here a compilation-end diagnostic (RS1037). Consequence: IDEs do not surface
            // these live while typing unless full solution analysis is enabled. Build output — which
            // is what matters for the AI-assisted workflow this analyzer targets — is unaffected.
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            // Definition files are AdditionalFiles, which are only reachable from a compilation action.
            context.RegisterCompilationAction(Analyze);
        }

        private static void Analyze(CompilationAnalysisContext context)
        {
            var definitions = DefinitionContext.Create(context.Options.AdditionalFiles, context.CancellationToken);

            foreach (var schema in definitions.FormSchemas)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var declared = schema.Root.Attribute("PermissionModelId");
                if (declared is not null && !string.IsNullOrWhiteSpace(declared.Value))
                    continue;

                // Point at PermissionModelId when it is present but blank, and at ProgId when the
                // attribute is absent entirely — that is the nearest thing on the element the reader
                // can act on, and it beats Location.None, which points the reader at nothing.
                var anchor = declared ?? schema.Root.Attribute("ProgId");
                var location = anchor is not null ? schema.CreateLocation(anchor) : Location.None;

                context.ReportDiagnostic(Diagnostic.Create(Rule, location, schema.ProgId));
            }
        }
    }
}
