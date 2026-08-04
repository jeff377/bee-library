using Bee.Business.Form;

namespace Bee.Business
{
    /// <summary>
    /// Minimal resolver — returns the framework's own type for a reserved progId and
    /// <see cref="FormBusinessObject"/> for everything else, consulting no registry.
    /// Reserved for tests and hosts that intentionally bypass
    /// <see cref="ProgramSettingsBoTypeResolver"/>; the framework default is
    /// <see cref="ProgramSettingsBoTypeResolver"/>, wired up by <c>AddBeeFramework</c>.
    /// </summary>
    /// <remarks>
    /// The reserved progIds are still honoured here. Without that this resolver would hand back a
    /// form business object for <c>System</c>, and the first login would fail as "method not
    /// found" — a symptom that points at the API layer rather than at the resolver in use.
    /// </remarks>
    public sealed class DefaultBoTypeResolver : IBoTypeResolver
    {
        /// <inheritdoc/>
        public Type Resolve(string progId)
            => ReservedProgIds.Find(progId)?.DefaultType ?? typeof(FormBusinessObject);
    }
}
