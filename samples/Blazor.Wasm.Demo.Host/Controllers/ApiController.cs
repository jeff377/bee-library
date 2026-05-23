using Bee.Api.AspNetCore.Controllers;

namespace Blazor.Wasm.Demo.Host.Controllers;

/// <summary>
/// Concrete JSON-RPC endpoint used by the Wasm client (<c>RemoteApiProvider</c>
/// targets <c>/api</c> from the same origin). The framework's
/// <see cref="ApiServiceController"/> declares <c>[Route("api")]</c>, so a host only
/// needs an empty subclass to publish the endpoint.
/// </summary>
public class ApiController : ApiServiceController
{
}
