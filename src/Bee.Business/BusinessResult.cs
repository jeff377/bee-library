using Bee.Definition.Collections;

namespace Bee.Business
{
    /// <summary>
    /// Base class for business object output results (pure POCO, no serialization).
    /// </summary>
    public abstract class BusinessResult
    {
        /// <summary>
        /// Gets or sets the output parameter collection.
        /// </summary>
        public ParameterCollection Parameters { get; set; }
    }
}
