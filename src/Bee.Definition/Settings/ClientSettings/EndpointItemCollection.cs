using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of service endpoint list items.
    /// </summary>
    public class EndpointItemCollection : CollectionBase<EndpointItem>
    {
        /// <summary>
        /// Adds a service endpoint item to the collection.
        /// </summary>
        /// <param name="name">The service endpoint name.</param>
        /// <param name="endpoint">The service endpoint location. Use a URL for remote connections or a local path for local connections.</param>
        public EndpointItem Add(string name, string endpoint)
        {
            var item = new EndpointItem(name, endpoint);
            this.Add(item);
            return item;
        }
    }
}
