using Bee.Base;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business
{
    /// <summary>
    /// The progIds the framework owns. Unlike an application progId, a reserved one has a business
    /// object the framework ships and an API surface callers depend on, so it is held to stricter
    /// rules on both registration and resolution.
    /// </summary>
    public static class ReservedProgIds
    {
        /// <summary>
        /// Gets every reserved progId binding.
        /// </summary>
        public static IReadOnlyList<ReservedProgIdBinding> All { get; } =
        [
            new(SysProgIds.System, typeof(SystemBusinessObject), typeof(SystemBusinessObject)),
            new(SysProgIds.AuditLog, typeof(LogBusinessObject), typeof(LogBusinessObject)),
            new(SysProgIds.AuditRule, typeof(AuditRuleBusinessObject), typeof(FormBusinessObject)),
        ];

        /// <summary>
        /// Returns the binding for the supplied progId, or <c>null</c> when it is an ordinary
        /// application progId.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public static ReservedProgIdBinding? Find(string progId)
            => All.FirstOrDefault(binding => StringUtilities.IsEquals(binding.ProgId, progId));
    }
}
