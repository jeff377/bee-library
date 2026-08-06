using Bee.Base;
using Bee.Business.AuditLog;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business
{
    /// <summary>
    /// What the framework requires of one reserved progId: the business object it resolves to when
    /// the registry does not name one, and the base every candidate type must derive from.
    /// </summary>
    /// <param name="ProgId">The reserved program identifier.</param>
    /// <param name="DefaultType">The framework's own business object for this progId.</param>
    /// <param name="ExpectedBaseType">
    /// The base a registered type must derive from. Narrower than <see cref="BusinessObject"/>,
    /// which every progId already satisfies: binding <c>System</c> to some form business object
    /// would pass the general check and still be entirely wrong, because the caller goes on to
    /// invoke <c>Login</c> on it.
    /// </param>
    public sealed record ReservedProgIdBinding(string ProgId, Type DefaultType, Type ExpectedBaseType)
    {
        /// <summary>
        /// Gets the assembly-qualified name of <see cref="DefaultType"/>, in the form
        /// <c>ProgramItem.BusinessObject</c> stores.
        /// </summary>
        public string DefaultTypeName
            => $"{DefaultType.FullName}, {DefaultType.Assembly.GetName().Name}";
    }

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
