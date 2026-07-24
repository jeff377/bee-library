using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Factory that creates <see cref="ApiPayloadJsonConverter{T}"/> for any <see cref="ApiPayload"/>-derived type.
    /// </summary>
    public class ApiPayloadJsonConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(ApiPayload).IsAssignableFrom(typeToConvert) && !typeToConvert.IsAbstract;
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(ApiPayloadJsonConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
