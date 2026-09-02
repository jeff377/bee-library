using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Definition.Filters
{
    /// <summary>
    /// Custom JSON converter for a single polymorphic <see cref="FilterNode"/> member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WARNING: Without this, a member whose declared type is <see cref="FilterNode"/> — such as a
    /// list request's filter — serializes to nothing but its discriminator. System.Text.Json binds
    /// the declared type, so a <see cref="FilterGroup"/> assigned to a <see cref="FilterNode"/>
    /// property writes <c>{"kind":"Group"}</c> and loses its operator and its whole subtree,
    /// silently and without error.
    /// </para>
    /// <para>
    /// <see cref="FilterNodeCollectionJsonConverter"/> covers the same hierarchy inside a
    /// collection. That one was enough for as long as MessagePack carried every encoded body — the
    /// API layer's filter node formatter handles the polymorphism there — so the gap only showed on
    /// the JSON paths.
    /// </para>
    /// <para>
    /// WARNING: Apply this with <c>[JsonConverter]</c> on the <b>property</b>, never on
    /// <see cref="FilterNode"/> itself. A converter attribute on the base type is inherited by
    /// every subclass, so writing a <see cref="FilterGroup"/> would re-enter this converter and
    /// recurse until the stack dies — a segfault, not a catchable exception. Placed on the
    /// property it governs only that member, and the delegation below serializes the concrete
    /// type, which carries no such attribute.
    /// </para>
    /// <para>
    /// NOTE: That delegation is also why no hand-written member list is needed here, unlike the
    /// MessagePack counterpart: the concrete type serializes through the normal contract, so the
    /// ignore conditions and naming policy of whatever options are in play still apply.
    /// </para>
    /// </remarks>
    public class FilterNodeJsonConverter : JsonConverter<FilterNode>
    {
        /// <summary>
        /// Serializes a <see cref="FilterNode"/> using its concrete type.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The node to serialize.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, FilterNode value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        /// <summary>
        /// Deserializes a <see cref="FilterNode"/>, choosing the concrete type from the
        /// <see cref="FilterNode.Kind"/> discriminator.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The target object type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The deserialized node, or null.</returns>
        public override FilterNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Unexpected token type '{reader.TokenType}' when reading FilterNode.");

            using var doc = JsonDocument.ParseValue(ref reader);
            return ReadNode(doc.RootElement, options);
        }

        /// <summary>
        /// Binds one node element to its concrete type. Kept in step with
        /// <see cref="FilterNodeCollectionJsonConverter"/>, which reads the same discriminator.
        /// </summary>
        internal static FilterNode? ReadNode(JsonElement element, JsonSerializerOptions options)
        {
            if (!element.TryGetProperty("kind", out var kindProp))
            {
                // No Kind property — default to FilterCondition, matching the collection converter.
                return element.Deserialize<FilterCondition>(options);
            }

            var kindValue = kindProp.ValueKind == JsonValueKind.String
                ? Enum.Parse<FilterNodeKind>(kindProp.GetString()!)
                : (FilterNodeKind)kindProp.GetInt32();

            return kindValue switch
            {
                FilterNodeKind.Condition => element.Deserialize<FilterCondition>(options),
                FilterNodeKind.Group => element.Deserialize<FilterGroup>(options),
                _ => throw new JsonException($"Unknown FilterNodeKind: {kindValue}")
            };
        }
    }
}
