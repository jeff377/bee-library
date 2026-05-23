using Bee.Business.System;
using Bee.Definition;

namespace Bee.Samples.Shared;

/// <summary>
/// Sample <see cref="SystemBusinessObject"/> that accepts a single hard-coded credential
/// (<see cref="DemoCredentials.UserId"/> + <see cref="DemoCredentials.Password"/>) without
/// touching the <c>st_user</c> table. Drops the seed-user / system-table requirements
/// that a real deployment would have, keeping the Blazor demos to a single SQLite file.
/// </summary>
public sealed class DemoAuthenticatingSystemBusinessObject : SystemBusinessObject
{
    public DemoAuthenticatingSystemBusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
        : base(ctx, accessToken, isLocalCall)
    {
    }

    /// <inheritdoc/>
    protected override bool AuthenticateUser(LoginArgs args, out string userName)
    {
        if (args is { UserId: DemoCredentials.UserId, Password: DemoCredentials.Password })
        {
            userName = DemoCredentials.DisplayName;
            return true;
        }
        userName = string.Empty;
        return false;
    }
}
