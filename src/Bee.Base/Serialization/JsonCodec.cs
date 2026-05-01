using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// JSON serialization codec. Round-trips objects via <see cref="JsonSerializer"/>
    /// with framework defaults (camelCase, indented) and dispatches lifecycle hooks for
    /// objects implementing <see cref="IObjectSerialize"/> / <see cref="IObjectSerializeProcess"/>.
    /// </summary>
    public static class JsonCodec
    {
        /// <summary>
        /// Gets the JSON serializer options.
        /// </summary>
        /// <param name="ignoreDefaultValue">Whether to ignore default values.</param>
        /// <param name="ignoreNullValue">Whether to ignore null values.</param>
        private static JsonSerializerOptions GetJsonSerializerOptions(bool ignoreDefaultValue, bool ignoreNullValue)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Ignore default/null values
            if (ignoreDefaultValue && ignoreNullValue)
                options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            else if (ignoreNullValue)
                options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            // Custom converters for DataSet/DataTable with full metadata preservation
            options.Converters.Add(new DataTableJsonConverter());
            options.Converters.Add(new DataSetJsonConverter());
            // Use string representation for enum types
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="ignoreDefaultValue">Whether to ignore default values.</param>
        /// <param name="ignoreNullValue">Whether to ignore null values.</param>
        /// <param name="includeTypeName">This parameter is no longer used and will be removed in a future version.</param>
        public static string Serialize(object value, bool ignoreDefaultValue = true, bool ignoreNullValue = true, bool includeTypeName = true)
        {
            // Pre-serialization operations
            SerializationLifecycle.NotifyBefore(SerializeFormat.Json, value);

            // Serialize to JSON string
            var options = GetJsonSerializerOptions(ignoreDefaultValue, ignoreNullValue);
            string json = JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), options);

            // Post-serialization operations
            SerializationLifecycle.NotifyAfter(SerializeFormat.Json, value);
            return json;
        }

        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <param name="includeTypeName">This parameter is no longer used and will be removed in a future version.</param>
        public static T? Deserialize<T>(string json, bool includeTypeName = true)
        {
            // Deserialize the JSON string
            var options = GetJsonSerializerOptions(true, false);
            var value = JsonSerializer.Deserialize<T>(json, options);
            // Post-deserialization operations
            SerializationLifecycle.NotifyAfterDeserialize(SerializeFormat.Json, value);
            return value;
        }

        /// <summary>
        /// Serializes an object to a JSON file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The JSON file path.</param>
        public static void SerializeToFile(object value, string filePath)
        {
            string json = Serialize(value, true);
            FileUtilities.FileWriteText(filePath, json);
            // Set the serialization-bound file
            if (value is IObjectSerializeFile objectSerializeFile) { objectSerializeFile.SetObjectFilePath(filePath); }
        }

        /// <summary>
        /// Deserializes a JSON file to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="filePath">The JSON file path.</param>
        public static T? DeserializeFromFile<T>(string filePath)
        {
            try
            {
                string json = FileUtilities.FileReadText(filePath);
                T? value = Deserialize<T>(json);
                // Set the serialization-bound file
                if (value is IObjectSerializeFile objectSerializeFile) { objectSerializeFile.SetObjectFilePath(filePath); }
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"DeserializeFromFile Error: {ex.Message}\nFileName: {filePath}", ex);
            }
        }
    }
}
