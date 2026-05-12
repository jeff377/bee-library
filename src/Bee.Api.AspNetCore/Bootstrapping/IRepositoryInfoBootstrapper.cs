using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Api.AspNetCore.Bootstrapping
{
    /// <summary>
    /// Marker service used to eager-resolve and assign the static
    /// <see cref="RepositoryInfo.SystemFactory"/> / <see cref="RepositoryInfo.FormFactory"/>
    /// holders once during host startup. Phase 4 keeps <see cref="RepositoryInfo"/> as a
    /// transitional static holder; Phase 5/6 will migrate consumers to constructor injection
    /// and delete this bootstrapper.
    /// </summary>
    public interface IRepositoryInfoBootstrapper { }

    internal sealed class RepositoryInfoBootstrapper : IRepositoryInfoBootstrapper
    {
        public RepositoryInfoBootstrapper(ISystemRepositoryFactory systemFactory, IFormRepositoryFactory formFactory)
        {
            RepositoryInfo.SystemFactory = systemFactory;
            RepositoryInfo.FormFactory = formFactory;
        }
    }
}
