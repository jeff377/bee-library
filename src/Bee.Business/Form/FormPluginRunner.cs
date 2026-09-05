using Bee.Definition;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Business.Form
{
    /// <summary>
    /// Runs one program's plugin chain for the duration of a single save or delete call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Plugins are constructed on demand: a plugin is created the first time the stage it is bound
    /// to runs, and not at all otherwise. A save therefore never constructs a delete-stage plugin.
    /// This is safe precisely because a plugin binds to exactly one stage — there is no later stage
    /// that would have to find the same object.
    /// </para>
    /// <para>
    /// Instances are per operation and never shared between calls, so a plugin needs no locking.
    /// What it cannot do is carry state to another stage: that is a second class, with no shared
    /// field between them, and anything the later stage needs it must read or recompute.
    /// </para>
    /// <para>
    /// A chain that binds no plugin, or none at the stage being run, costs a single branch: nothing
    /// is constructed and the pipeline is untouched.
    /// </para>
    /// </remarks>
    public sealed class FormPluginRunner
    {
        private readonly FormPluginChain _chain;
        private readonly IBeeContext _ctx;
        private readonly Guid _accessToken;
        private readonly string _progId;
        private FormBusinessPlugin?[]? _instances;

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
        /// Runs every plugin bound to <see cref="PluginStage.BeforeSave"/>, in order.
        /// </summary>
        /// <param name="context">The save context.</param>
        public void RunBeforeSave(SaveContext context)
            => Run(PluginStage.BeforeSave, plugin => plugin.BeforeSave(context));

        /// <summary>
        /// Runs every plugin bound to <see cref="PluginStage.AfterSave"/>, in order.
        /// </summary>
        /// <param name="context">The save context.</param>
        public void RunAfterSave(SaveContext context)
            => Run(PluginStage.AfterSave, plugin => plugin.AfterSave(context));

        /// <summary>
        /// Runs every plugin bound to <see cref="PluginStage.BeforeDelete"/>, in order.
        /// </summary>
        /// <param name="context">The delete context.</param>
        public void RunBeforeDelete(DeleteContext context)
            => Run(PluginStage.BeforeDelete, plugin => plugin.BeforeDelete(context));

        /// <summary>
        /// Runs every plugin bound to <see cref="PluginStage.AfterDelete"/>, in order.
        /// </summary>
        /// <param name="context">The delete context.</param>
        public void RunAfterDelete(DeleteContext context)
            => Run(PluginStage.AfterDelete, plugin => plugin.AfterDelete(context));

        private void Run(PluginStage stage, Action<FormBusinessPlugin> invoke)
        {
            if (_chain.IsEmpty) { return; }

            var entries = _chain.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Stage == stage)
                    invoke(Instance(i));
            }
        }

        /// <summary>
        /// Returns the instance for one entry, constructing it the first time its stage runs.
        /// </summary>
        private FormBusinessPlugin Instance(int index)
        {
            _instances ??= new FormBusinessPlugin?[_chain.Entries.Length];

            // ActivatorUtilities so a plugin may declare its own injected dependencies beyond the
            // three positional arguments, the same way a custom repository can.
            return _instances[index] ??= (FormBusinessPlugin)ActivatorUtilities.CreateInstance(
                _ctx.Services, _chain.Entries[index].Type, _ctx, _accessToken, _progId);
        }
    }
}
