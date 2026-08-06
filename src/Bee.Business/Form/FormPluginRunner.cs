using Bee.Definition;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Business.Form
{
    /// <summary>
    /// Runs one program's plugin chain for the duration of a single save or delete call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is where the one-instance-per-operation guarantee lives.</b> Plugins are constructed
    /// on first use and reused for every later stage of the same call, so a plugin overriding both
    /// <c>BeforeSave</c> and <c>AfterSave</c> can carry state between them in an instance field.
    /// Constructing per stage instead would silently drop that state — with no compile error — so
    /// the behaviour is covered by tests rather than left to inspection.
    /// </para>
    /// <para>
    /// A chain that binds no plugin, or none implementing the stage being run, costs a single
    /// branch: nothing is constructed and the pipeline is untouched.
    /// </para>
    /// </remarks>
    public sealed class FormPluginRunner
    {
        private readonly FormPluginChain _chain;
        private readonly IBeeContext _ctx;
        private readonly Guid _accessToken;
        private readonly string _progId;
        private FormBusinessPlugin[]? _instances;

        /// <summary>
        /// Initializes a new <see cref="FormPluginRunner"/>.
        /// </summary>
        /// <param name="chain">The resolved plugin chain.</param>
        /// <param name="ctx">The per-call business context.</param>
        /// <param name="accessToken">The access token of the call being extended.</param>
        /// <param name="progId">The program identifier.</param>
        internal FormPluginRunner(FormPluginChain chain, IBeeContext ctx, Guid accessToken, string progId)
        {
            _chain = chain ?? throw new ArgumentNullException(nameof(chain));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _accessToken = accessToken;
            _progId = progId ?? throw new ArgumentNullException(nameof(progId));
        }

        /// <summary>Gets the chain this runner executes.</summary>
        public FormPluginChain Chain => _chain;

        /// <summary>
        /// Runs every plugin implementing <see cref="FormPluginStage.BeforeSave"/>, in order.
        /// </summary>
        /// <param name="context">The save context.</param>
        public void RunBeforeSave(SaveContext context)
            => Run(FormPluginStage.BeforeSave, plugin => plugin.BeforeSave(context));

        /// <summary>
        /// Runs every plugin implementing <see cref="FormPluginStage.AfterSave"/>, in order.
        /// </summary>
        /// <param name="context">The save context.</param>
        public void RunAfterSave(SaveContext context)
            => Run(FormPluginStage.AfterSave, plugin => plugin.AfterSave(context));

        /// <summary>
        /// Runs every plugin implementing <see cref="FormPluginStage.BeforeDelete"/>, in order.
        /// </summary>
        /// <param name="context">The delete context.</param>
        public void RunBeforeDelete(DeleteContext context)
            => Run(FormPluginStage.BeforeDelete, plugin => plugin.BeforeDelete(context));

        /// <summary>
        /// Runs every plugin implementing <see cref="FormPluginStage.AfterDelete"/>, in order.
        /// </summary>
        /// <param name="context">The delete context.</param>
        public void RunAfterDelete(DeleteContext context)
            => Run(FormPluginStage.AfterDelete, plugin => plugin.AfterDelete(context));

        private void Run(FormPluginStage stage, Action<FormBusinessPlugin> invoke)
        {
            if (_chain.IsEmpty || !_chain.HasStage(stage)) { return; }

            var entries = _chain.Entries;
            var instances = EnsureInstances();
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Stages.Contains(stage))
                    invoke(instances[i]);
            }
        }

        /// <summary>
        /// Constructs the whole chain on first use. All of it, not just the stage being run: the
        /// point of one instance per operation is that a later stage finds the same object, which
        /// only holds if construction is tied to the operation rather than to a stage.
        /// </summary>
        private FormBusinessPlugin[] EnsureInstances()
        {
            if (_instances != null) { return _instances; }

            var entries = _chain.Entries;
            var instances = new FormBusinessPlugin[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                // ActivatorUtilities so a plugin may declare its own injected dependencies beyond
                // the three positional arguments, the same way a custom repository can.
                instances[i] = (FormBusinessPlugin)ActivatorUtilities.CreateInstance(
                    _ctx.Services, entries[i].Type, _ctx, _accessToken, _progId);
            }

            _instances = instances;
            return instances;
        }
    }
}
