
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
}
