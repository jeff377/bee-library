using System.Reflection;
using Bee.Definition;

namespace Bee.Business.Form
{
    /// <summary>
    /// The resolved, ordered plugin chain of one program: which types run, and which stages each of
    /// them actually implements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built once per <c>(customizationCode, progId)</c> and cached, so the reflection that decides
    /// "does this type override <c>AfterSave</c>" is paid once rather than per call. It holds no
    /// instances — those are per operation, and come from <see cref="CreateRunner"/>.
    /// </para>
    /// <para>
    /// Knowing the stages up front does two jobs. It keeps a stage from walking plugins that would
    /// do nothing, and it answers "which plugins run at which point" for a maintenance tool —
    /// something the definition file cannot show, because it names types without naming stages.
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
        /// Builds a chain from the resolved plugin types, in execution order.
        /// </summary>
        /// <param name="types">The plugin types, already validated as deriving from <see cref="FormBusinessPlugin"/>.</param>
        public static FormPluginChain Create(IReadOnlyList<Type> types)
        {
            ArgumentNullException.ThrowIfNull(types);
            if (types.Count == 0) { return Empty; }

            var entries = new Entry[types.Count];
            for (int i = 0; i < types.Count; i++)
                entries[i] = new Entry(types[i], OverriddenStages(types[i]));
            return new FormPluginChain(entries);
        }

        /// <summary>Gets the plugin types in execution order.</summary>
        public IReadOnlyList<Type> Types => Array.ConvertAll(_entries, e => e.Type);

        /// <summary>Gets whether the program has no plugins bound.</summary>
        public bool IsEmpty => _entries.Length == 0;

        /// <summary>
        /// Gets whether any plugin in the chain implements the given stage.
        /// </summary>
        /// <param name="stage">The pipeline stage.</param>
        public bool HasStage(FormPluginStage stage)
            => Array.Exists(_entries, e => e.Stages.Contains(stage));

        /// <summary>
        /// Gets the plugin types that implement the given stage, in execution order. Intended for
        /// maintenance tooling that has to show what actually runs where.
        /// </summary>
        /// <param name="stage">The pipeline stage.</param>
        public IReadOnlyList<Type> TypesForStage(FormPluginStage stage)
            => _entries.Where(e => e.Stages.Contains(stage)).Select(e => e.Type).ToArray();

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
        /// Determines which stages a plugin type overrides, by checking whether each virtual method
        /// is still the one declared on <see cref="FormBusinessPlugin"/>.
        /// </summary>
        private static FormPluginStage[] OverriddenStages(Type type)
        {
            var stages = new List<FormPluginStage>(4);
            if (Overrides(type, nameof(FormBusinessPlugin.BeforeSave))) { stages.Add(FormPluginStage.BeforeSave); }
            if (Overrides(type, nameof(FormBusinessPlugin.AfterSave))) { stages.Add(FormPluginStage.AfterSave); }
            if (Overrides(type, nameof(FormBusinessPlugin.BeforeDelete))) { stages.Add(FormPluginStage.BeforeDelete); }
            if (Overrides(type, nameof(FormBusinessPlugin.AfterDelete))) { stages.Add(FormPluginStage.AfterDelete); }
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

        /// <summary>One plugin: its type and the stages it implements.</summary>
        internal readonly struct Entry
        {
            public Entry(Type type, FormPluginStage[] stages)
            {
                Type = type;
                Stages = stages;
            }

            public Type Type { get; }

            public FormPluginStage[] Stages { get; }
        }
    }
}
