using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

namespace QuickStart.Server.BusinessObjects;

/// <summary>
/// Demo business object that echoes a message. Inherits <see cref="FormBusinessObject"/>
/// because the framework dispatches non-"System" progIds through
/// <c>IBusinessObjectFactory.CreateFormBusinessObject</c>, which expects the
/// <c>(IBeeContext, Guid, string, bool)</c> constructor signature.
/// </summary>
/// <remarks>
/// The <see cref="ApiAccessControlAttribute"/> marks <see cref="Echo"/> as
/// public and anonymous so the QuickStart console can call it without an
/// access token or encryption hand-shake.
/// </remarks>
public class EchoBusinessObject : FormBusinessObject
{
    /// <summary>
    /// Initializes a new instance. The signature mirrors
    /// <see cref="FormBusinessObject"/> so <c>Activator.CreateInstance</c> in
    /// <c>BusinessObjectFactory</c> can resolve the constructor.
    /// </summary>
    /// <param name="ctx">The per-call context.</param>
    /// <param name="accessToken">The access token (ignored for anonymous calls).</param>
    /// <param name="progId">The program identifier (expected to be "Echo").</param>
    /// <param name="isLocalCall">Whether the call originates from a local source.</param>
    public EchoBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, progId, isLocalCall)
    {
    }

    /// <summary>
    /// Returns the input message decorated with a server-side prefix.
    /// </summary>
    /// <param name="args">The input arguments.</param>
    [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
    public virtual EchoResult Echo(EchoArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new EchoResult
        {
            Response = $"echo: {args.Message}",
            ServerTime = DateTime.UtcNow,
        };
    }
}
