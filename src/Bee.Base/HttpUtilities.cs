using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// Utility library for HTTP operations.
    /// </summary>
    public static class HttpUtilities
    {
        private static readonly ConcurrentDictionary<string, HttpClient> _clientMap = new ConcurrentDictionary<string, HttpClient>();

        /// <summary>
        /// Creates or retrieves a <see cref="HttpClient"/> instance for the given host.
        /// </summary>
        /// <param name="fullUrl">The full URL of the API, e.g. https://api.example.com/v1/login.</param>
        /// <returns>A shared <see cref="HttpClient"/> instance that reuses the same connection pool.</returns>
        /// <remarks>
        /// This method creates a unique cache key based on the URL's <c>Schema + Host + Port</c> to avoid
        /// creating too many <c>HttpClient</c> instances for the same host, preventing Socket Exhaustion and DNS cache issues.
        /// </remarks>
        private static HttpClient GetOrCreateClient(string fullUrl)
        {
            var baseUri = new Uri(fullUrl);
            string cacheKey = $"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}";

            return _clientMap.GetOrAdd(cacheKey, _ =>
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
                };
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/"),
                    Timeout = TimeSpan.FromSeconds(30)
                };
            });
        }

        /// <summary>
        /// Determines whether the specified input is a valid URL.
        /// </summary>
        /// <param name="input">The input URL to check.</param>
        public static bool IsUrl(string input)
        {
            Uri? uriResult;
            return Uri.TryCreate(input, UriKind.Absolute, out uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Asynchronously probes whether the specified endpoint is reachable over the network.
        /// </summary>
        /// <param name="endpoint">The endpoint URL to probe.</param>
        /// <param name="timeout">The probe timeout. Defaults to 5 seconds.</param>
        /// <returns>
        /// True if the server returns any HTTP response (including 4xx/5xx status codes);
        /// false on DNS failure, connection refused, or timeout.
        /// </returns>
        /// <remarks>
        /// Uses an HTTP HEAD request as a low-cost probe. Any HTTP response — including 404 or 405 —
        /// proves the host is alive and routing requests, so it is treated as "reachable".
        /// Use this only for transport-level reachability; verifying that the endpoint actually
        /// implements the expected service contract is the caller's responsibility.
        /// </remarks>
        public static async Task<bool> IsEndpointReachableAsync(string endpoint, TimeSpan? timeout = null)
        {
            using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
            try
            {
                HttpClient client = GetOrCreateClient(endpoint);
                using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
                using var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        /// <summary>
        /// Asynchronously sends a POST request.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="body">The JSON string to pass in the request body.</param>
        /// <param name="headers">Custom request headers.</param>
        public static async Task<string> PostAsync(string endpoint, string body, NameValueCollection? headers = null)
        {
            HttpClient client = GetOrCreateClient(endpoint);

            // Send POST request
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                if (headers != null)
                {
                    foreach (string key in headers)
                    {
                        request.Headers.TryAddWithoutValidation(key, headers[key]);
                    }
                }

                using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();  // Verify success
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false); // Read response content
                }
            }
        }

        /// <summary>
        /// Asynchronously sends a GET request.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="headers">Custom request headers.</param>
        public static async Task<string> GetAsync(string endpoint, NameValueCollection? headers = null)
        {
            HttpClient client = GetOrCreateClient(endpoint);

            // Send GET request
            using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint))
            {
                if (headers != null)
                {
                    foreach (string key in headers)
                    {
                        request.Headers.TryAddWithoutValidation(key, headers[key]);
                    }
                }

                using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();  // Verify success
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);  // Read response content
                }
            }
        }
    }
}
