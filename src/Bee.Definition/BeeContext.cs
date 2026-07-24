using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Storage;

namespace Bee.Definition
{
    /// <summary>
    /// Default <see cref="IBeeContext"/> implementation; a plain POCO assembled
    /// by <c>BusinessObjectFactory</c> at BO construction time.
    /// </summary>
    public sealed class BeeContext : IBeeContext
    {
        /// <inheritdoc/>
        public required IDefineAccess DefineAccess { get; init; }

        /// <inheritdoc/>
        public required ISessionInfoService SessionInfoService { get; init; }

        /// <inheritdoc/>
        public required ILanguageService LanguageService { get; init; }

        /// <inheritdoc/>
        public required IBusinessObjectFactory BoFactory { get; init; }

        /// <inheritdoc/>
        public required IServiceProvider Services { get; init; }
    }
}
