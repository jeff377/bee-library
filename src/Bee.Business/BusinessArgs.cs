using Bee.Definition.Collections;

namespace Bee.Business
{
    /// <summary>
    /// Base class for business object input arguments (pure POCO, no serialization).
    /// </summary>
    public abstract class BusinessArgs
    {
        /// <summary>
        /// Gets or sets the input parameter collection.
        /// </summary>
        public ParameterCollection Parameters { get; set; }
    }
}
