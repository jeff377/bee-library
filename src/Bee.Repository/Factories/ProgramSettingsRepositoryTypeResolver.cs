using Bee.Base;
using Bee.Definition.Customization;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Form;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default <see cref="IRepositoryTypeResolver"/>: reads <see cref="ProgramItem.Repository"/> from
    /// <see cref="ProgramSettings"/>, applying the tenant customization overlay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every failure throws</b>, the same policy the business-object axis applies to a
    /// declared binding. A name that will not load would otherwise send this program's reads
    /// and writes through the framework's own SQL after an author replaced that logic on
    /// purpose; a fallback would not avert the failure, only postpone it to a point where the
    /// data is already wrong. An <b>empty</b> <see cref="ProgramItem.Repository"/> is not a
    /// failure — it declares nothing, and the framework's own repository serves the
    /// schema-driven CRUD.
    /// </para>
    /// <para>
    /// Unlike the business-object resolver this holds no type cache, so a definition reload
    /// takes effect on the next call with no invalidation machinery. What that costs is one
    /// <see cref="AssemblyLoader.GetType(string)"/> per resolution, and its expensive half — the
    /// assembly load — is already cached inside <see cref="AssemblyLoader"/>.
    /// </para>
    /// </remarks>
    public sealed class ProgramSettingsRepositoryTypeResolver : IRepositoryTypeResolver
    {
        private readonly IDefineAccess _defineAccess;
        private readonly ICustomizeDefineReader? _customizeReader;
        private readonly ISessionInfoService? _sessionInfoService;

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsRepositoryTypeResolver"/>.
        /// </summary>
        /// <param name="defineAccess">Loads the base <see cref="ProgramSettings"/>.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay, so every progId resolves against the base registry.</param>
        /// <param name="sessionInfoService">Reads the session's customization code; <c>null</c> has the same effect as a host with no sessions — the base registry applies.</param>
        public ProgramSettingsRepositoryTypeResolver(
            IDefineAccess defineAccess,
            ICustomizeDefineReader? customizeReader = null,
            ISessionInfoService? sessionInfoService = null)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _customizeReader = customizeReader;
            _sessionInfoService = sessionInfoService;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the registry names a type that will not load, or one that does not derive
        /// from <see cref="DataFormRepository"/>.
        /// </exception>
        public Type Resolve(Guid accessToken, string progId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);

            var item = FindProgramItem(accessToken, progId);
            if (item == null || StringUtilities.IsEmpty(item.Repository))
                return typeof(DataFormRepository);

            Type? type;
            try
            {
                type = AssemblyLoader.GetType(item.Repository);
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                throw Unloadable(progId, item.Repository, ex);
            }

            if (type == null)
                throw Unloadable(progId, item.Repository, inner: null);

            if (!typeof(DataFormRepository).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"ProgramSettings binds progId '{progId}' to repository '{item.Repository}', " +
                    $"which does not derive from {typeof(DataFormRepository).FullName}. " +
                    "A form repository must extend the framework's own so the CRUD surface stays intact.");
            }

            return type;
        }

        /// <summary>
        /// Reads the registry entry for a progId, letting a tenant customization override the
        /// bindings it names while the rest keep their base values — the same per-progId,
        /// per-property granularity the business-object axis uses, through the same
        /// <see cref="CustomizeOverlay"/> a client runs, so both ends agree.
        /// </summary>
        /// <remarks>
        /// The per-property part matters most here: a customization that replaces only
        /// <see cref="ProgramItem.BusinessObject"/> must not silently return this program's data
        /// access to the generic repository, which is what a whole-entry replacement would do — an
        /// empty <see cref="ProgramItem.Repository"/> is a legal "use the default" and would never
        /// be reported as an error.
        /// </remarks>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">The program identifier.</param>
        private ProgramItem? FindProgramItem(Guid accessToken, string progId)
        {
            ProgramSettings? baseSettings;
            try
            {
                baseSettings = _defineAccess.GetProgramSettings();
            }
            catch (FileNotFoundException)
            {
                // No ProgramSettings.xml means no progId is bound to anything; a customization entry
                // may still apply below. Same tolerance as the business-object resolver, so a host
                // can adopt the binding feature without shipping a base registry first.
                baseSettings = null;
            }

            string customizeId = GetCustomizeId(accessToken);
            ProgramSettings? custSettings = null;
            if (StringUtilities.IsNotEmpty(customizeId) && _customizeReader is not null)
                custSettings = _customizeReader.GetCustomizeProgramSettings(customizeId);

            return CustomizeOverlay.FindProgramItem(custSettings, baseSettings, progId);
        }

        /// <summary>
        /// Reads the session's tenant customization code. The session is the only accepted source:
        /// the code selects which tenant's definition files are read, so honouring a caller-supplied
        /// value would be a cross-tenant read.
        /// </summary>
        /// <param name="accessToken">The access token identifying the session.</param>
        private string GetCustomizeId(Guid accessToken)
        {
            if (accessToken == Guid.Empty || _sessionInfoService == null)
                return string.Empty;
            return _sessionInfoService.Get(accessToken)?.CustomizeId ?? string.Empty;
        }

        private static InvalidOperationException Unloadable(string progId, string typeName, Exception? inner)
            => new(
                $"ProgramSettings binds progId '{progId}' to repository '{typeName}', which cannot be loaded. " +
                "Fix the assembly-qualified type name, or clear the attribute to use the framework default.",
                inner);
    }
}
