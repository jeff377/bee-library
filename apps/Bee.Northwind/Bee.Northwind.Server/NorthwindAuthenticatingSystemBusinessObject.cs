using Bee.Business.System;
using Bee.Definition;

namespace Bee.Northwind.Server;

/// <summary>
/// <see cref="SystemBusinessObject"/> that accepts a single hard-coded credential
/// (<see cref="NorthwindCredentials.UserId"/> + <see cref="NorthwindCredentials.Password"/>)
/// without touching the <c>st_user</c> table, keeping the demo to a single SQLite file.
/// </summary>
public sealed class NorthwindAuthenticatingSystemBusinessObject : SystemBusinessObject
{
    public NorthwindAuthenticatingSystemBusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
        : base(ctx, accessToken, isLocalCall)
    {
    }

    /// <inheritdoc/>
    protected override bool AuthenticateUser(LoginArgs args, out string userName)
    {
        if (args is { UserId: NorthwindCredentials.UserId, Password: NorthwindCredentials.Password })
        {
            userName = NorthwindCredentials.DisplayName;
            return true;
        }
        userName = string.Empty;
        return false;
    }
}
