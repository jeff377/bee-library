using Bee.Api.AspNetCore.Controllers;

namespace Bee.Northwind.Server.Controllers;

/// <summary>
/// Concrete JSON-RPC endpoint. The framework's <see cref="ApiServiceController"/> already
/// declares <c>[Route("api")]</c> and the POST handler, so a host only needs an empty
/// subclass to publish the endpoint.
/// </summary>
public class ApiController : ApiServiceController
{
}
