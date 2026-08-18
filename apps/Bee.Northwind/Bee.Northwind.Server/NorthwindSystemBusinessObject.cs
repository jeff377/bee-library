using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Identity;
using Microsoft.Extensions.DependencyInjection;

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
    /// <remarks>
    /// WARNING: both fields have to be stamped here. Taking this shortcut means
    /// <c>SessionCompanyBinder</c> never runs, and that is the only place the framework copies
    /// <c>CompanyInfo.CustomizeId</c> onto the session. Setting <c>CompanyId</c> alone leaves the
    /// customization code empty, which every customization lookup treats as "this deployment has
    /// no customization layer" and short-circuits — silently, with no error anywhere.
    /// </remarks>
    public override LoginResult Login(LoginArgs args)
    {
        var result = base.Login(args);

        // The session was just created by base.Login; stamp the company context onto it. The
        // customization code is read back off the company rather than named again here, so the
        // demo has exactly one place that decides which customization the company maps onto.
        var company = Services.GetRequiredService<ICompanyInfoService>().Get(NorthwindCredentials.CompanyId)
            ?? throw new InvalidOperationException(
                $"Company '{NorthwindCredentials.CompanyId}' is not known to ICompanyInfoService.");

        var session = SessionInfoService.Get(result.AccessToken);
        session.CompanyId = company.CompanyId;
        session.CustomizeId = company.CustomizeId;
        SessionInfoService.Set(session);

        return result;
    }
}
