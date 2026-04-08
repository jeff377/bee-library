using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Core
{
    /// <summary>
    /// Utility library for HTTP operations.
    /// </summary>
    public static class HttpFunc
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
                return new HttpClient
                {
                    BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/")
                };
            });
        }

        /// <summary>
        /// Determines whether the specified input is a valid URL.
        /// </summary>
        /// <param name="input">The input URL to check.</param>
        public static bool IsUrl(string input)
        {
            Uri uriResult;
            return Uri.TryCreate(input, UriKind.Absolute, out uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Asynchronously sends a POST request.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="body">The JSON string to pass in the request body.</param>
        /// <param name="headers">Custom request headers.</param>
        public static async Task<string> PostAsync(string endpoint, string body, NameValueCollection headers = null)
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
        public static async Task<string> GetAsync(string endpoint, NameValueCollection headers = null)
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
