namespace Bee.Definition.Logging
{
    /// <summary>
    /// Resolution helpers for <see cref="AuditRuleMode"/>.
    /// </summary>
    public static class AuditRuleModeExtensions
    {
        /// <summary>
        /// Resolves the three-state mode against the deployment-wide default for its axis.
        /// </summary>
        /// <param name="mode">The per-form mode.</param>
        /// <param name="inherited">
        /// The deployment-wide default this axis uses (<see cref="Bee.Definition.Settings.AuditLogOptions.ChangeEnabled"/> or
        /// <see cref="Bee.Definition.Settings.AuditLogOptions.AccessEnabled"/>), returned when <paramref name="mode"/> is
        /// <see cref="AuditRuleMode.Inherit"/>.
        /// </param>
        /// <returns><c>true</c> when the axis should be recorded for this form.</returns>
        /// <remarks>
        /// The master switch <see cref="Bee.Definition.Settings.AuditLogOptions.Enabled"/> is deliberately not a parameter: it is
        /// checked by the caller before this is reached, so that a deployment with auditing off
        /// never pays for a rule lookup.
        /// </remarks>
        public static bool Resolve(this AuditRuleMode mode, bool inherited)
        {
            return mode switch
            {
                AuditRuleMode.On => true,
                AuditRuleMode.Off => false,
                _ => inherited,
            };
        }
    }
}
