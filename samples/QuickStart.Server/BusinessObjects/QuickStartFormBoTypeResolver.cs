using Bee.Business;
using Bee.Business.Form;

namespace QuickStart.Server.BusinessObjects;

/// <summary>
/// Resolves the QuickStart demo's progIds to concrete BO types. Overrides the
/// framework's <see cref="DefaultFormBoTypeResolver"/> (which always returns
/// <see cref="FormBusinessObject"/>) by intercepting <c>"Echo"</c>; every other
/// progId still falls back to the base class so the framework's FormSchema-driven
/// CRUD path keeps working.
/// </summary>
public sealed class QuickStartFormBoTypeResolver : IFormBoTypeResolver
{
    /// <inheritdoc/>
    public Type Resolve(string progId) => progId switch
    {
        "Echo" => typeof(EchoBusinessObject),
        _ => typeof(FormBusinessObject),
    };
}
