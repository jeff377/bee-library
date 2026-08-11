using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// JSON serialization codec. Round-trips objects via <see cref="JsonSerializer"/>
    /// with framework defaults (camelCase) and dispatches lifecycle hooks for
    /// objects implementing <see cref="IObjectSerialize"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WARNING: the option instances below are shared and must stay shared.
    /// <see cref="JsonSerializerOptions"/> is where System.Text.Json caches the contract it builds
    /// for each type it meets — property list, converter resolution, naming policy. Handing the
    /// serializer a fresh instance per call throws that cache away every time, so every call pays
    /// full reflection again. Building them once is the whole point of this shape; do not move the
    /// construction back inside the methods.
    /// </para>
    /// <para>
    /// NOTE: wire output is compact and file output is indented. Indentation is for a human reading
    /// a definition file on disk, and costs both bytes and time on a request path that no one reads.
    /// </para>
    /// </remarks>
    public static class JsonCodec
    {
        private static readonly JsonSerializerOptions s_compactIgnoreDefault =
            CreateOptions(JsonIgnoreCondition.WhenWritingDefault, writeIndented: false);

        private static readonly JsonSerializerOptions s_compactIgnoreNull =
            CreateOptions(JsonIgnoreCondition.WhenWritingNull, writeIndented: false);

        private static readonly JsonSerializerOptions s_compactKeepAll =
            CreateOptions(JsonIgnoreCondition.Never, writeIndented: false);

        private static readonly JsonSerializerOptions s_indentedIgnoreDefault =
            CreateOptions(JsonIgnoreCondition.WhenWritingDefault, writeIndented: true);

        private static readonly JsonSerializerOptions s_indentedIgnoreNull =
            CreateOptions(JsonIgnoreCondition.WhenWritingNull, writeIndented: true);

        private static readonly JsonSerializerOptions s_indentedKeepAll =
            CreateOptions(JsonIgnoreCondition.Never, writeIndented: true);

        /// <summary>
        /// Builds one of the shared option instances.
        /// </summary>
        /// <param name="ignoreCondition">The condition under which a property is omitted.</param>
        /// <param name="writeIndented">Whether the output is indented for human reading.</param>
        private static JsonSerializerOptions CreateOptions(JsonIgnoreCondition ignoreCondition, bool writeIndented)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIndented,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = ignoreCondition
            };

            // Custom converters for DataSet/DataTable with full metadata preservation
            options.Converters.Add(new DataTableJsonConverter());
            options.Converters.Add(new DataSetJsonConverter());
            // Use string representation for enum types
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        /// <summary>
        /// Selects the shared option instance matching the requested behaviour.
        /// </summary>
        /// <param name="ignoreDefaultValue">Whether to ignore default values.</param>
        /// <param name="ignoreNullValue">Whether to ignore null values.</param>
        /// <param name="writeIndented">Whether the output is indented for human reading.</param>
        private static JsonSerializerOptions GetJsonSerializerOptions(
            bool ignoreDefaultValue, bool ignoreNullValue, bool writeIndented)
        {
            if (ignoreDefaultValue && ignoreNullValue)
                return writeIndented ? s_indentedIgnoreDefault : s_compactIgnoreDefault;

            if (ignoreNullValue)
                return writeIndented ? s_indentedIgnoreNull : s_compactIgnoreNull;

            return writeIndented ? s_indentedKeepAll : s_compactKeepAll;
        }

        /// <summary>
        /// Serializes an object to a compact JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="ignoreDefaultValue">Whether to ignore default values.</param>
        /// <param name="ignoreNullValue">Whether to ignore null values.</param>
        /// <param name="includeTypeName">This parameter is no longer used and will be removed in a future version.</param>
        public static string Serialize(object value, bool ignoreDefaultValue = true, bool ignoreNullValue = true, bool includeTypeName = true)
        {
            return SerializeCore(value, ignoreDefaultValue, ignoreNullValue, writeIndented: false);
        }

        /// <summary>
        /// Serializes an object to a JSON string with the requested indentation.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="ignoreDefaultValue">Whether to ignore default values.</param>
        /// <param name="ignoreNullValue">Whether to ignore null values.</param>
        /// <param name="writeIndented">Whether the output is indented for human reading.</param>
        private static string SerializeCore(object value, bool ignoreDefaultValue, bool ignoreNullValue, bool writeIndented)
        {
            // Pre-serialization operations
            SerializationLifecycle.NotifyBefore(value);

            // Serialize to JSON string
            var options = GetJsonSerializerOptions(ignoreDefaultValue, ignoreNullValue, writeIndented);
            string json = JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), options);

            // Post-serialization operations
            SerializationLifecycle.NotifyAfter(value);
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
            var options = GetJsonSerializerOptions(true, false, writeIndented: false);
            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Serializes an object to a JSON file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The JSON file path.</param>
        /// <remarks>File output is indented, unlike <see cref="Serialize"/>, because a person reads it.</remarks>
        public static void SerializeToFile(object value, string filePath)
        {
            string json = SerializeCore(value, ignoreDefaultValue: true, ignoreNullValue: true, writeIndented: true);
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
