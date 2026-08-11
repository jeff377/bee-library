using Bee.Business.System;
using Bee.Definition;

namespace Bee.Northwind.Server;

/// <summary>
/// <see cref="SystemBusinessObject"/> that auto-enters the demo's single company at login.
/// </summary>
/// <remarks>
/// Authentication is <em>not</em> overridden: the framework's own <c>st_user</c> check is used, and
/// the demo account is seeded by <see cref="NorthwindSchemaSeeder"/>. Comparing an account and a
/// password is the same operation in every deployment, so nothing about it belongs here.
/// <para>
/// What does belong here is the company context, and only because this demo takes a shortcut. The
/// full <c>EnterCompany</c> path validates the company exists and is enabled, checks the user's
/// access in <c>st_user_company</c>, then snapshots roles and the employee context. A
/// single-company demo whose forms declare no permission models has none of that to validate, so
/// stamping <c>SessionInfo.CompanyId</c> directly is the minimal equivalent. A deployment with more
/// than one company calls <c>EnterCompany</c> and deletes this class.
/// </para>
/// </remarks>
public sealed class NorthwindSystemBusinessObject : SystemBusinessObject
{
    public NorthwindSystemBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
        : base(ctx, accessToken, progId, isLocalCall)
    {
    }

    /// <inheritdoc/>
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
