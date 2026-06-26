using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of service endpoint list items.
    /// </summary>
    public class EndpointItemCollection : CollectionBase<EndpointItem>
    {
    }

    /// <summary>
    /// Provides extension methods for <see cref="EndpointItemCollection"/>.
    /// </summary>
    public static class EndpointItemCollectionExtensions
    {
        /// <summary>
        /// Adds a service endpoint item to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="name">The service endpoint name.</param>
        /// <param name="endpoint">The service endpoint location. Use a URL for remote connections or a local path for local connections.</param>
        public static EndpointItem Add(this EndpointItemCollection? collection, string name, string endpoint)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var item = new EndpointItem(name, endpoint);
            collection.Add(item);
            return item;
        }
    }
}
