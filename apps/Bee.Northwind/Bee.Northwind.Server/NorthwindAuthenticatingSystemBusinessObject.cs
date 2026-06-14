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

    /// <summary>
    /// Logs in and auto-enters the single demo company so company-scoped forms resolve their
    /// database. The full <c>EnterCompany</c> path validates <c>st_user_company</c> access and
    /// snapshots roles / employee context — none of which the hard-coded demo has; setting
    /// <c>SessionInfo.CompanyId</c> directly is the minimal equivalent for a single-company demo
    /// whose forms declare no permission models.
    /// </summary>
    public override LoginResult Login(LoginArgs args)
    {
        var result = base.Login(args);

        // The session was just created by base.Login; stamp the company context onto it.
        var session = SessionInfoService.Get(result.AccessToken);
        session.CompanyId = NorthwindCredentials.CompanyId;
        SessionInfoService.Set(session);

        return result;
    }
}
