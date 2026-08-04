using Bee.Business;
using Bee.Business.Form;

namespace QuickStart.Server.BusinessObjects;

/// <summary>
/// Resolves the QuickStart demo's progIds to concrete BO types. Overrides the
/// framework's <see cref="DefaultBoTypeResolver"/> (which always returns
/// <see cref="FormBusinessObject"/>) by intercepting <c>"Echo"</c>; every other
/// progId still falls back to the base class so the framework's FormSchema-driven
/// CRUD path keeps working.
/// </summary>
public sealed class QuickStartBoTypeResolver : IBoTypeResolver
{
    /// <inheritdoc/>
    /// <remarks>
    /// The reserved progIds are delegated to <see cref="ReservedProgIds"/> rather than falling into
    /// the <c>FormBusinessObject</c> default. A custom resolver that swallows them leaves
    /// <c>System</c> resolving to an object with no <c>Login</c>, and the host refuses to start —
    /// which is the intended outcome, but easier to avoid than to diagnose.
    /// </remarks>
    public Type Resolve(string progId) => progId switch
    {
        "Echo" => typeof(EchoBusinessObject),
        _ => ReservedProgIds.Find(progId)?.DefaultType ?? typeof(FormBusinessObject),
    };
}
