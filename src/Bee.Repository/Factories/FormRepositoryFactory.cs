using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default implementation of <see cref="IFormRepositoryFactory"/>, now a thin adapter over
    /// <see cref="IRepositoryFactory"/>.
    /// </summary>
    /// <remarks>
    /// Kept only so consumers can move across one at a time.
    /// </remarks>
    public class FormRepositoryFactory : IFormRepositoryFactory
    {
        private readonly IRepositoryFactory _factory;

        /// <summary>
        /// Initializes a new <see cref="FormRepositoryFactory"/>.
        /// </summary>
        /// <param name="factory">The unified repository factory every method delegates to.</param>
        public FormRepositoryFactory(IRepositoryFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken)
            => _factory.CreateFormRepository<IDataFormRepository>(accessToken, progId);

        /// <inheritdoc/>
        public IReportFormRepository CreateReportFormRepository(string progId)
            => _factory.Create<IReportFormRepository>();
    }
}
