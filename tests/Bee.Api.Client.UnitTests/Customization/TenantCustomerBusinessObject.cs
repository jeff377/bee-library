using Bee.Business.Form;
using Bee.Definition;

namespace Bee.Api.Client.UnitTests.Customization
{
    /// <summary>
    /// Stands in for a tenant-supplied business object: the customization
    /// <c>ProgramSettings.xml</c> written by <see cref="TenantCustomizationFixture"/> binds
    /// <c>Customer</c> to this type, so resolving it proves the binding came from the
    /// customization layer rather than the base one.
    /// </summary>
    /// <remarks>
    /// Top-level (not nested in the test class) because the binding names it as
    /// <c>"Namespace.Type, Assembly"</c> and <c>AssemblyLoader</c> resolves that form; a nested type
    /// would need the <c>+</c> spelling and read as an accident waiting to happen.
    /// </remarks>
    public class TenantCustomerBusinessObject : FormBusinessObject
    {
        /// <summary>
        /// Initializes a new <see cref="TenantCustomerBusinessObject"/>.
        /// </summary>
        /// <param name="context">The business context.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public TenantCustomerBusinessObject(IBeeContext context, Guid accessToken, string progId, bool isLocalCall = true)
            : base(context, accessToken, progId, isLocalCall)
        {
        }
    }
}
