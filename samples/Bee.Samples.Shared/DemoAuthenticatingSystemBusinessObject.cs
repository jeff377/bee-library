using Bee.Business.System;
using Bee.Definition;

namespace Bee.Samples.Shared;

/// <summary>
/// Sample <see cref="SystemBusinessObject"/> that accepts a single hard-coded credential
/// (<see cref="DemoCredentials.UserId"/> + <see cref="DemoCredentials.Password"/>) instead of
/// checking the password stored in <c>st_user</c>, which is what lets the demos run without
/// password hashing or user maintenance.
/// </summary>
/// <remarks>
/// It replaces the credential check and nothing else. The rest of the login path is unchanged, so
/// the common system tables are still required: signing in reads the user's locale from
/// <c>st_user</c> and writes the session seed to <c>st_session</c>. <c>DemoSchemaSeeder</c>
/// creates both and seeds the matching row.
/// </remarks>
public sealed class DemoAuthenticatingSystemBusinessObject : SystemBusinessObject
{
    public DemoAuthenticatingSystemBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, progId, isLocalCall)
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
