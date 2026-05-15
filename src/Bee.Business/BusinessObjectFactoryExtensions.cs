using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business
{
    /// <summary>
    /// Typed convenience wrappers around <see cref="IBusinessObjectFactory"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IBusinessObjectFactory"/> lives in <c>Bee.Definition</c> and returns
    /// <see cref="object"/> to avoid a reverse dependency from <c>Bee.Definition</c> to
    /// <c>Bee.Business</c> (where <see cref="IFormBusinessObject"/> /
    /// <see cref="ISystemBusinessObject"/> live). These extension methods are defined in
    /// <c>Bee.Business</c> so the cast can be encapsulated once for callers that already
    /// reference <c>Bee.Business</c> (typically other business objects making BO-to-BO calls).
    /// <para>
    /// One method per axis: each axis has a single canonical interface and the concrete
    /// type is selected by <c>progId</c> at runtime, so a generic overload would not add
    /// value over the manual cast it would otherwise replace.
    /// </para>
    /// </remarks>
    public static class BusinessObjectFactoryExtensions
    {
        /// <summary>
        /// Creates a form-level business object and returns it as <see cref="IFormBusinessObject"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        public static IFormBusinessObject CreateFormBO(
            this IBusinessObjectFactory factory,
            Guid accessToken,
            string progId,
            bool isLocalCall = true)
        {
            ArgumentNullException.ThrowIfNull(factory);
            return (IFormBusinessObject)factory.CreateFormBusinessObject(accessToken, progId, isLocalCall);
        }

        /// <summary>
        /// Creates a system-level business object and returns it as <see cref="ISystemBusinessObject"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        public static ISystemBusinessObject CreateSystemBO(
            this IBusinessObjectFactory factory,
            Guid accessToken,
            bool isLocalCall = true)
        {
            ArgumentNullException.ThrowIfNull(factory);
            return (ISystemBusinessObject)factory.CreateSystemBusinessObject(accessToken, isLocalCall);
        }
    }
}
