using Bee.Api.AspNetCore.Controllers;

namespace Bee.LoadTests.Serving
{
    /// <summary>
    /// The JSON-RPC endpoint a Remote run measures against.
    /// </summary>
    /// <remarks>
    /// <see cref="ApiServiceController"/> already declares the route and the POST handler, so an
    /// empty subclass is all it takes to publish the endpoint.
    /// </remarks>
    public class ApiController : ApiServiceController
    {
    }
}
