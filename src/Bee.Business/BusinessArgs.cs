using Bee.Definition.Collections;

namespace Bee.Business
{
    /// <summary>
    /// Base class for business object input arguments (pure POCO, no serialization).
    /// </summary>
    public abstract class BusinessArgs
    {
        private ParameterCollection _parameters;

        /// <summary>
        /// Gets or sets the input parameter collection.
        /// The collection is lazily initialized on first access so handlers can
        /// safely call <c>args.Parameters.Add(...)</c> without null checks.
        /// </summary>
        public ParameterCollection Parameters
        {
            get => _parameters ??= new ParameterCollection();
            set => _parameters = value;
        }
    }
}
