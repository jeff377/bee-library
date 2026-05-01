using System.ComponentModel;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Represents the standard API data structure, supporting serialization, compression, and encryption.
    /// </summary>
    [JsonConverter(typeof(ApiPayloadJsonConverterFactory))]
    public abstract class ApiPayload : IObjectSerialize
    {
        #region IObjectSerialize 介面

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [JsonIgnore]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            if (Value is IObjectSerialize objectSerialize)
            {
                objectSerialize.SetSerializeState(serializeState);
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the payload format (plain, encoded, or encrypted).
        /// </summary>
        [JsonPropertyName("format")]
        public PayloadFormat Format { get; internal set; } = PayloadFormat.Plain;

        /// <summary>
        /// Gets or sets the payload value.
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the type name of the payload value, used to specify the target type during deserialization.
        /// </summary>
        [JsonPropertyName("type")]
        [DefaultValue("")]
        public string TypeName { get; set; } = string.Empty;
    }
}
