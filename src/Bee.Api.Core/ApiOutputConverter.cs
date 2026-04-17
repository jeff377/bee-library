using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// Converts BO result objects to API response types by naming convention.
    /// When the Executor receives a BO result (e.g., PingResult), this converter
    /// resolves the corresponding API response type (e.g., PingResponse) via reflection
    /// and copies matching properties.
    /// </summary>
    /// <remarks>
    /// This is the output counterpart of <see cref="ApiInputConverter"/>.
    /// The naming convention is: {Action}Result (BO) → {Action}Response (API).
    /// Resolved types are cached to avoid repeated reflection overhead.
    /// </remarks>
    public static class ApiOutputConverter
    {
        // Cache: BO Result Type → API Response Type.
        // A value of typeof(void) is used as a sentinel to indicate "no matching type found",
        // since ConcurrentDictionary does not accept null values.
        private static readonly ConcurrentDictionary<Type, Type> _cache = new();
        private static readonly Type _noMatch = typeof(void);

        private const string ResultSuffix = "Result";
        private const string ResponseSuffix = "Response";

        /// <summary>
        /// Converts a BO result to the corresponding API response type.
        /// Returns the original value if no matching API response type is found.
        /// </summary>
        /// <param name="boResult">The BO result object to convert.</param>
        /// <returns>
        /// A new instance of the API response type with matching properties copied,
        /// or the original value if no convention-matched type exists.
        /// </returns>
        public static object? Convert(object boResult)
        {
            if (boResult == null) return null;

            var boType = boResult.GetType();
            var responseType = _cache.GetOrAdd(boType, ResolveResponseType);

            if (responseType == _noMatch) return boResult;

            return ApiInputConverter.Convert(boResult, responseType);
        }

        /// <summary>
        /// Converts a JSON-RPC result value to the specified type.
        /// Handles both direct type matches and <see cref="JsonElement"/> deserialization
        /// (which occurs when the response is received over HTTP).
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="value">The raw result value from <c>JsonRpcResult.Value</c>.</param>
        /// <returns>The value converted to type <typeparamref name="T"/>.</returns>
        public static T? ConvertResultValue<T>(object value)
        {
            if (value is T typed) return typed;
            if (value is JsonElement element)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<T>(element.GetRawText(), options);
            }
            return (T)value;
        }

        /// <summary>
        /// Resolves the API response type from a BO result type using naming convention.
        /// e.g., PingResult → PingResponse (in Bee.Api.Core assembly).
        /// </summary>
        /// <param name="boType">The BO result type.</param>
        /// <returns>The matching API response type, or the sentinel type if not found.</returns>
        private static Type ResolveResponseType(Type boType)
        {
            if (!boType.Name.EndsWith(ResultSuffix))
                return _noMatch;

            var responseName = boType.Name[..^ResultSuffix.Length] + ResponseSuffix;

            // Search in the Bee.Api.Core assembly (where API response types live)
            var apiCoreAssembly = typeof(ApiOutputConverter).Assembly;
            return apiCoreAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == responseName && !t.IsAbstract)
                ?? _noMatch;
        }
    }
}
