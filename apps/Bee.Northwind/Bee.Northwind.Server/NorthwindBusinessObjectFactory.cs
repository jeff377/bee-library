using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Storage;

namespace Bee.Northwind.Server;

/// <summary>
/// <see cref="IBusinessObjectFactory"/> wrapper that routes system-level calls to
/// <see cref="NorthwindAuthenticatingSystemBusinessObject"/> (so login works without
/// seeding <c>st_user</c>) while keeping the framework defaults for form-level CRUD dispatch.
/// </summary>
public sealed class NorthwindBusinessObjectFactory : IBusinessObjectFactory
{
    private readonly IServiceProvider _services;
    private readonly IDefineAccess _defineAccess;
    private readonly ISessionInfoService _sessionInfoService;
    private readonly ILanguageService _languageService;
    private readonly IFormBoTypeResolver _resolver;

    public NorthwindBusinessObjectFactory(
        IServiceProvider services,
        IDefineAccess defineAccess,
        ISessionInfoService sessionInfoService,
        ILanguageService languageService,
        IFormBoTypeResolver resolver)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc/>
    public object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
    {
        var ctx = BuildContext();
        return new NorthwindAuthenticatingSystemBusinessObject(ctx, accessToken, isLocalCall);
    }

    /// <inheritdoc/>
    public object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
    {
        var type = _resolver.Resolve(progId);
        var ctx = BuildContext();
        return Activator.CreateInstance(type, ctx, accessToken, progId, isLocalCall)!;
    }

    private BeeContext BuildContext() => new()
    {
        DefineAccess = _defineAccess,
        SessionInfoService = _sessionInfoService,
        LanguageService = _languageService,
        BoFactory = this,
        Services = _services,
    };
}
