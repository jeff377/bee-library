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
    /// The non-generic overloads are the common case and return the canonical interface
    /// for each axis. The generic overloads exist to support specialised BO interfaces
    /// (for example a future <c>IEmployeeBusinessObject : IFormBusinessObject</c>) without
    /// requiring a new extension method per specialisation.
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
            => factory.CreateFormBO<IFormBusinessObject>(accessToken, progId, isLocalCall);

        /// <summary>
        /// Creates a form-level business object and returns it as a specialised
        /// <see cref="IFormBusinessObject"/>-derived interface.
        /// </summary>
        /// <typeparam name="T">The expected interface type; must derive from <see cref="IFormBusinessObject"/>.</typeparam>
        /// <param name="factory">The factory.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        public static T CreateFormBO<T>(
            this IBusinessObjectFactory factory,
            Guid accessToken,
            string progId,
            bool isLocalCall = true)
            where T : IFormBusinessObject
        {
            ArgumentNullException.ThrowIfNull(factory);
            return (T)factory.CreateFormBusinessObject(accessToken, progId, isLocalCall);
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
            => factory.CreateSystemBO<ISystemBusinessObject>(accessToken, isLocalCall);

        /// <summary>
        /// Creates a system-level business object and returns it as a specialised
        /// <see cref="ISystemBusinessObject"/>-derived interface.
        /// </summary>
        /// <typeparam name="T">The expected interface type; must derive from <see cref="ISystemBusinessObject"/>.</typeparam>
        /// <param name="factory">The factory.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        public static T CreateSystemBO<T>(
            this IBusinessObjectFactory factory,
            Guid accessToken,
            bool isLocalCall = true)
            where T : ISystemBusinessObject
        {
            ArgumentNullException.ThrowIfNull(factory);
            return (T)factory.CreateSystemBusinessObject(accessToken, isLocalCall);
        }
    }
}
