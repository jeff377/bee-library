using System.Net.Http.Headers;
using Bee.Api.Core;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Bee.Api.Core.Messages;

namespace Bee.Api.AspNetCore.Controllers
{
    /// <summary>
    /// Base controller class for handling JSON-RPC API requests in ASP.NET Core.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public abstract class ApiServiceController : ControllerBase
    {
        /// <summary>
        /// Gets a value indicating whether the current environment is the development environment.
        /// </summary>
        protected bool IsDevelopment =>
            HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();

        /// <summary>
        /// Handles HTTP POST requests and executes the corresponding API service.
        /// </summary>
        /// <param name="apiKey">The API key header value, bound from the <c>X-Api-Key</c> request header.</param>
        /// <param name="authorization">The authorization header value, bound from the <c>Authorization</c> request header.</param>
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<IActionResult> PostAsync(
            [FromHeader(Name = ApiHeaders.ApiKey)] string? apiKey = null,
            [FromHeader(Name = ApiHeaders.Authorization)] string? authorization = null)
        {
            // Read and parse the JSON-RPC request
            JsonRpcRequest request;
            try
            {
                request = await ReadRequestAsync();
            }
            catch (JsonRpcException ex)
            {
                return CreateErrorResponse(ex.HttpStatusCode, ex.ErrorCode, ex.RpcMessage);
            }

            // Validate the API key and authorization
            var result = ValidateAuthorization(request, apiKey, authorization);
            if (!result.IsValid)
            {
                return CreateErrorResponse(StatusCodes.Status401Unauthorized, result.Code, result.ErrorMessage, request.Id);
            }

            // Execute the corresponding API method
            return await HandleRequestAsync(result.AccessToken, request);
        }

        /// <summary>
        /// Reads and parses the JSON-RPC request from the HTTP request body.
        /// </summary>
        /// <returns>A successfully parsed <see cref="JsonRpcRequest"/> instance.</returns>
        /// <exception cref="JsonRpcException">Thrown when the body is empty or the format is invalid.</exception>
        protected virtual async Task<JsonRpcRequest> ReadRequestAsync()
        {
            if (!MediaTypeHeaderValue.TryParse(HttpContext.Request.ContentType, out var mediaType) ||
                mediaType.MediaType == null ||
                !mediaType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonRpcException(StatusCodes.Status415UnsupportedMediaType,
                    JsonRpcErrorCode.InvalidRequest, "Unsupported media type");
            }

            Request.EnableBuffering();
            Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new JsonRpcException(StatusCodes.Status400BadRequest,
                    JsonRpcErrorCode.InvalidRequest, "Empty request body");
            }

            try
            {
                var request = SerializeFunc.JsonToObject<JsonRpcRequest>(json);
                if (request == null || string.IsNullOrWhiteSpace(request.Method))
                {
                    throw new JsonRpcException(StatusCodes.Status400BadRequest,
                        JsonRpcErrorCode.InvalidRequest, "Missing method");
                }

                return request;
            }
            catch (JsonRpcException) { throw; }
            catch (Exception ex)
            {
                throw new JsonRpcException(StatusCodes.Status400BadRequest,
                    JsonRpcErrorCode.ParseError, $"Invalid JSON format: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates the API authorization information.
        /// </summary>
        /// <param name="request">The JSON-RPC request.</param>
        /// <param name="apiKey">The API key extracted from the <c>X-Api-Key</c> header.</param>
        /// <param name="authorization">The raw authorization header value.</param>
        /// <returns>The authorization validation result.</returns>
        protected virtual ApiAuthorizationResult ValidateAuthorization(JsonRpcRequest request, string? apiKey, string? authorization)
        {
            var context = new ApiAuthorizationContext
            {
                ApiKey = apiKey ?? string.Empty,
                Authorization = authorization ?? string.Empty,
                Method = request.Method
            };

            var validator = ApiServiceOptions.AuthorizationValidator;
            return validator.Validate(context);
        }

        /// <summary>
        /// Handles the JSON-RPC request and executes the corresponding API method.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="request">The JSON-RPC request model.</param>
        protected virtual async Task<IActionResult> HandleRequestAsync(Guid accessToken, JsonRpcRequest request)
        {
            try
            {
                var executor = new JsonRpcExecutor(accessToken);
                var result = await executor.ExecuteAsync(request);
                return new ContentResult
                {
                    Content = result.ToJson(),
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                var rootEx = BaseFunc.UnwrapException(ex);
                string message = IsDevelopment
                    ? rootEx.Message
                    : string.Empty;

                return CreateErrorResponse(StatusCodes.Status500InternalServerError, JsonRpcErrorCode.InternalError,
                    "Internal server error", request.Id, message);
            }
        }

        /// <summary>
        /// Creates a JSON-RPC formatted error response.
        /// </summary>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <param name="code">The JSON-RPC error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="id">The corresponding request ID, or null if not applicable.</param>
        /// <param name="data">Additional error data, or null if not applicable.</param>
        /// <returns>An <see cref="IActionResult"/> containing the error information.</returns>
        protected virtual IActionResult CreateErrorResponse(int httpStatusCode, JsonRpcErrorCode code, string message, string? id = null, string? data = null)
        {
            var response = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = (int)code,
                    Message = message,
                    Data = data
                }
            };
            return StatusCode(httpStatusCode, response);
        }

    }
}
