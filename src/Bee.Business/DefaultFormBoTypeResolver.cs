using Bee.Business.Form;

namespace Bee.Business
{
    /// <summary>
    /// Minimal resolver — always returns <see cref="FormBusinessObject"/>.
    /// Reserved for tests and hosts that intentionally bypass
    /// <see cref="ProgramSettingsFormBoTypeResolver"/>; the framework default is
    /// <see cref="ProgramSettingsFormBoTypeResolver"/>, wired up by <c>AddBeeFramework</c>.
    /// </summary>
    public sealed class DefaultFormBoTypeResolver : IFormBoTypeResolver
    {
        /// <inheritdoc/>
        public Type Resolve(string progId) => typeof(FormBusinessObject);
    }
}
