using Bee.Repository.Abstractions.Factories;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// Provides static access to the system and form repository factories.
    /// </summary>
    /// <remarks>
    /// Populated at host startup by <c>AddBeeFramework</c>; consumers in the business
    /// layer read these factories via static accessors. Future phases will migrate
    /// consumers to constructor-injected factories and remove this static holder.
    /// </remarks>
    public static class RepositoryInfo
    {
        /// <summary>
        /// Gets or sets the system repository factory.
        /// </summary>
        public static ISystemRepositoryFactory? SystemFactory { get; set; }

        /// <summary>
        /// Gets or sets the form repository factory.
        /// </summary>
        public static IFormRepositoryFactory? FormFactory { get; set; }
    }
}
