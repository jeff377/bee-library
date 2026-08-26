using System.Data.Common;
using Bee.Definition;
using Bee.Definition.Logging;
using Bee.Business.Form;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;
using Microsoft.Extensions.Logging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Maintenance business object for the per-form audit rules (<c>st_audit_rule</c>). An ordinary
    /// form business object apart from one duty: after a rule changes it evicts the company's
    /// cached snapshot and announces the change to other processes.
    /// </summary>
    /// <remarks>
    /// Without this the rules would still be read correctly, but only by a process that had not yet
    /// cached them — the snapshot has no expiry, so an edit would appear to do nothing until restart.
    /// </remarks>
    public class AuditRuleBusinessObject : FormBusinessObject
    {
        /// <summary>
        /// Initializes a new <see cref="AuditRuleBusinessObject"/>.
        /// </summary>
        /// <param name="ctx">The business context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">The program identifier.</param>
        public AuditRuleBusinessObject(IBeeContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId)
        {
        }

        /// <inheritdoc/>
        protected override void DoAfterSave(SaveContext context)
        {
            base.DoAfterSave(context);
            InvalidateRules();
        }

        /// <inheritdoc/>
        protected override void DoAfterDelete(DeleteContext context)
        {
            base.DoAfterDelete(context);
            InvalidateRules();
        }

        /// <summary>
        /// Evicts this session's company from the rule cache and announces the change to other
        /// processes.
        /// </summary>
        /// <remarks>
        /// Local eviction happens first and unconditionally: it is what makes the edit visible to
        /// the operator who just made it, and it works even where no cache-notify database is
        /// configured. The announcement is best-effort on top of that.
        /// </remarks>
        private void InvalidateRules()
        {
            string? companyId = SessionInfoService.Get(AccessToken)?.CompanyId;
            if (string.IsNullOrEmpty(companyId)) { return; }

            Services.GetService<IAuditRuleService>()?.Remove(companyId);

            try
            {
                Services.GetService<IRepositoryFactory>()?
                    .Create<IAuditRuleRepository>(AccessToken)
                    .NotifyRulesChanged(companyId);
            }
            catch (DbException ex)
            {
                // The rule change has already committed, so failing here would report a successful
                // save as an error. DbException covers every provider's exception type; the cost of
                // swallowing it is that other processes keep their cached rules until the next
                // change, which the local eviction above has already made moot for this one.
                LogNotifyFailure(ex, companyId);
            }
            catch (InvalidOperationException ex)
            {
                // Same reasoning: a missing or misconfigured cache-notify database is a deployment
                // problem to surface in the log, not a reason to fail the operator's save.
                LogNotifyFailure(ex, companyId);
            }
        }

        /// <summary>
        /// Records that the cross-process announcement failed.
        /// </summary>
        /// <param name="ex">The failure.</param>
        /// <param name="companyId">The company whose rules changed.</param>
        private void LogNotifyFailure(Exception ex, string companyId)
        {
            Services.GetService<ILogger<AuditRuleBusinessObject>>()?.LogError(
                ex, "Audit-rule cache-notify announcement failed for company '{CompanyId}'.", companyId);
        }
    }
}
