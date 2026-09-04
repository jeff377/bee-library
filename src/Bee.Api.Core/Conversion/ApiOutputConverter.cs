using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Api.Core.Conversion
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
        private static readonly ConcurrentDictionary<Type, Type> s_cache = new();
        private static readonly Type s_noMatch = typeof(void);
        /// <summary>
        /// Read options for JSON responses.
        /// </summary>
        /// <remarks>
        /// WARNING: <see cref="JsonStringEnumConverter"/> must stay, and must match whatever
        /// <see cref="Bee.Base.Serialization.JsonCodec"/> writes. That writer emits enums as names, so a reader without this
        /// converter throws on the first response property that happens to be an enum. The converter
        /// still accepts numeric values, so it only widens what can be read.
        /// </remarks>
        private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

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
            var responseType = s_cache.GetOrAdd(boType, ResolveResponseType);

            if (responseType == s_noMatch) return boResult;

            return ApiInputConverter.Convert(boResult, responseType);
        }

        /// <summary>
        /// Converts a JSON-RPC result value to the specified type.
        /// Handles both direct type matches and <see cref="JsonElement"/> deserialization
        /// (which occurs when the response is received over HTTP).
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="value">The raw result value from <c>Value</c>.</param>
        /// <returns>The value converted to type <typeparamref name="T"/>.</returns>
        public static T? ConvertResultValue<T>(object value)
        {
            if (value is T typed) return typed;
            if (value is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText(), s_caseInsensitiveOptions);
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
            if (!boType.Name.EndsWith(ResultSuffix, StringComparison.Ordinal))
                return s_noMatch;

            var responseName = boType.Name[..^ResultSuffix.Length] + ResponseSuffix;

            // Search in the Bee.Api.Core assembly (where API response types live)
            var apiCoreAssembly = typeof(ApiOutputConverter).Assembly;
            return apiCoreAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == responseName && !t.IsAbstract)
                ?? s_noMatch;
        }
    }
}
