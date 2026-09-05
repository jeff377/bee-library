using System.Reflection;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.Business.Form
{
    /// <summary>
    /// The resolved, ordered plugin chain of one program: which types run, and at which stage each
    /// of them runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built once per <c>(customizationCode, progId)</c> and cached, so the reflection that checks
    /// each type against its declaration is paid once rather than per call. It holds no instances —
    /// those are per operation, and come from <see cref="CreateRunner"/>.
    /// </para>
    /// <para>
    /// The stage comes from the definition file, which is what lets the file be read for what runs
    /// where. Reflection has not gone away, but it is now the <b>check</b> rather than the source:
    /// a type must override exactly the one stage its binding declares, and any disagreement
    /// refuses to build the chain. Nothing runs at a stage the file does not name, and nothing the
    /// file names is silently absent.
    /// </para>
    /// </remarks>
    public sealed class FormPluginChain
    {
        private readonly Entry[] _entries;

        /// <summary>An empty chain: no plugin is bound to the program.</summary>
        public static FormPluginChain Empty { get; } = new([]);

        private FormPluginChain(Entry[] entries)
        {
            _entries = entries;
        }

        /// <summary>
        /// Builds a chain from the resolved bindings, in execution order, rejecting any binding
        /// whose declared stage is not exactly what the type overrides.
        /// </summary>
        /// <param name="progId">The program identifier, for error messages.</param>
        /// <param name="bindings">The bindings, with types already validated as deriving from <see cref="FormBusinessPlugin"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a type overrides no stage, overrides more than one, or overrides a stage
        /// other than the one declared. The message names the stages the type actually overrides,
        /// so the fix is in the message rather than left to be worked out.
        /// </exception>
        public static FormPluginChain Create(string progId, IReadOnlyList<FormPluginBinding> bindings)
        {
            ArgumentNullException.ThrowIfNull(bindings);
            if (bindings.Count == 0) { return Empty; }

            var entries = new Entry[bindings.Count];
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                entries[i] = new Entry(binding.Type, Reconcile(progId, binding));
            }
            return new FormPluginChain(entries);
        }

        /// <summary>Gets the plugin types in execution order.</summary>
        public IReadOnlyList<Type> Types => Array.ConvertAll(_entries, e => e.Type);

        /// <summary>Gets whether the program has no plugins bound.</summary>
        public bool IsEmpty => _entries.Length == 0;

        /// <summary>
        /// Gets whether any plugin in the chain runs at the given stage.
        /// </summary>
        /// <param name="stage">The pipeline stage.</param>
        public bool HasStage(PluginStage stage)
            => Array.Exists(_entries, e => e.Stage == stage);

        /// <summary>
        /// Gets the plugin types that run at the given stage, in execution order. Intended for
        /// maintenance tooling that has to show what runs where.
        /// </summary>
        /// <param name="stage">The pipeline stage.</param>
        public IReadOnlyList<Type> TypesForStage(PluginStage stage)
            => _entries.Where(e => e.Stage == stage).Select(e => e.Type).ToArray();

        /// <summary>
        /// Creates the per-operation runner that instantiates the plugins and dispatches the stages.
        /// </summary>
        /// <param name="ctx">The per-call business context.</param>
        /// <param name="accessToken">The access token of the call being extended.</param>
        /// <param name="progId">The program identifier.</param>
        public FormPluginRunner CreateRunner(IBeeContext ctx, Guid accessToken, string progId)
            => new(this, ctx, accessToken, progId);

        /// <summary>
        /// Gets the entries, for the runner's use.
        /// </summary>
        internal Entry[] Entries => _entries;

        /// <summary>
        /// Checks one binding's declared stage against what the type overrides, returning the
        /// agreed stage.
        /// </summary>
        private static PluginStage Reconcile(string progId, FormPluginBinding binding)
        {
            var overridden = OverriddenStages(binding.Type);
            string typeName = DisplayName(binding.Type);

            if (overridden.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Program '{progId}' declares plugin '{typeName}', which overrides no stage and would never run.");
            }

            if (overridden.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Program '{progId}' declares plugin '{typeName}', which overrides " +
                    $"{string.Join(" and ", overridden)}. A plugin binds to exactly one stage — " +
                    "split it into one class per stage.");
            }

            var actual = overridden[0];

            if (binding.Stage == PluginStage.None)
            {
                throw new InvalidOperationException(
                    $"Program '{progId}' declares plugin '{typeName}' with no Stage. " +
                    $"The type overrides {actual} — declare Stage=\"{actual}\".");
            }

            if (binding.Stage != actual)
            {
                throw new InvalidOperationException(
                    $"Program '{progId}' declares plugin '{typeName}' as Stage=\"{binding.Stage}\", " +
                    $"but the type overrides {actual}. Declare Stage=\"{actual}\", or move the override.");
            }

            return actual;
        }

        /// <summary>
        /// Renders a type the way the definition file names it, so the message points at the text
        /// the author has to edit rather than at a fully qualified assembly identity.
        /// </summary>
        private static string DisplayName(Type type)
            => $"{type.FullName}, {type.Assembly.GetName().Name}";

        /// <summary>
        /// Determines which stages a plugin type overrides, by checking whether each virtual method
        /// is still the one declared on <see cref="FormBusinessPlugin"/>.
        /// </summary>
        private static PluginStage[] OverriddenStages(Type type)
        {
            var stages = new List<PluginStage>(4);
            if (Overrides(type, nameof(FormBusinessPlugin.BeforeSave))) { stages.Add(PluginStage.BeforeSave); }
            if (Overrides(type, nameof(FormBusinessPlugin.AfterSave))) { stages.Add(PluginStage.AfterSave); }
            if (Overrides(type, nameof(FormBusinessPlugin.BeforeDelete))) { stages.Add(PluginStage.BeforeDelete); }
            if (Overrides(type, nameof(FormBusinessPlugin.AfterDelete))) { stages.Add(PluginStage.AfterDelete); }
            return [.. stages];
        }

        /// <summary>
        /// Returns whether the most derived declaration of the named method is something other than
        /// the empty one on the base class.
        /// </summary>
        private static bool Overrides(Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            return method != null && method.DeclaringType != typeof(FormBusinessPlugin);
        }

        /// <summary>One plugin: its type and the single stage it runs at.</summary>
        internal readonly struct Entry
        {
            public Entry(Type type, PluginStage stage)
            {
                Type = type;
                Stage = stage;
            }

            public Type Type { get; }

            public PluginStage Stage { get; }
        }
    }
}
