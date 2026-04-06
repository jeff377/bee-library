using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Form
{
    /// <summary>
    /// Repository implementation for data forms.
    /// </summary>
    public class DataFormRepository : IDataFormRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFormRepository"/> class.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public DataFormRepository(string progId)
        {
            ProgId = progId;
        }

        /// <summary>
        /// Gets the program identifier.
        /// </summary>
        public string ProgId { get; }
    }
}
