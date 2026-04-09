using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bee.Core.Serialization
{
    /// <summary>
    /// Utility library for serialization operations.
    /// </summary>
    public static class SerializeFunc
    {
        /// <summary>
        /// Performs pre-serialization operations.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        /// <param name="value">The object to serialize.</param>
        private static void DoBeforeSerialize(SerializeFormat serializeFormat, object value)
        {
            // Notify before serialization
            if (value is IObjectSerializeProcess serializeProcess) { serializeProcess.BeforeSerialize(serializeFormat); }
            // Mark serialization state as started
            if (value is IObjectSerialize objectSerialize) { objectSerialize.SetSerializeState(SerializeState.Serialize); }
        }

        /// <summary>
        /// Performs post-serialization operations.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        /// <param name="value">The object that was serialized.</param>
        private static void DoAfterSerialize(SerializeFormat serializeFormat, object value)
        {
            // Mark serialization state as ended
            if (value is IObjectSerialize objectSerialize) { objectSerialize.SetSerializeState(SerializeState.None); }
            // Notify after serialization
            if (value is IObjectSerializeProcess objectSerializeProcess) { objectSerializeProcess.AfterSerialize(serializeFormat); }
        }

        /// <summary>
        /// Performs post-deserialization operations.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        /// <param name="value">The deserialized object.</param>
        private static void DoAfterDeserialize(SerializeFormat serializeFormat, object value)
        {
            // Notify after deserialization
            if (value is IObjectSerializeProcess objectSerializeProcess) { objectSerializeProcess.AfterDeserialize(serializeFormat); }
        }

        /// <summary>
        /// Serializes an object to an XML string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        public static string ObjectToXml(object value)
        {
            if (value == null)
                return string.Empty;

            // Pre-serialization operations
            DoBeforeSerialize(SerializeFormat.Xml, value);

            // Serialize and write to string
            string xml = string.Empty;
            using (UTF8StringWriter writer = new UTF8StringWriter())
            {
                var serializer = XmlSerializerCache.Get(value.GetType());
                serializer.Serialize(writer, value);
                xml = writer.ToString();
            }

            // Post-serialization operations
            DoAfterSerialize(SerializeFormat.Xml, value);
            return xml;
        }

        /// <summary>
        /// Deserializes an XML string to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="xml">The XML string.</param>
        public static T XmlToObject<T>(string xml)
        {
            return (T)XmlToObject(xml, typeof(T));
        }

        /// <summary>
        /// Deserializes an XML string to an object.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <param name="type">The object type.</param>
        public static object XmlToObject(string xml, Type type)
        {
            if (StrFunc.IsEmpty(xml))
                return default;

            object value;
            using (StringReader reader = new StringReader(xml))
            {
                var serializer = XmlSerializerCache.Get(type);
                value = serializer.Deserialize(reader);
            }

            // Post-deserialization operations
            DoAfterDeserialize(SerializeFormat.Xml, value);
            return value;
        }

        /// <summary>
        /// Serializes an object to an XML file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The XML file path.</param>
        public static void ObjectToXmlFile(object value, string filePath)
        {
            string sXml;

            sXml = ObjectToXml(value);
            FileFunc.FileWriteText(filePath, sXml);
            // Set the serialization-bound file
            if (value is IObjectSerializeFile) { (value as IObjectSerializeFile).SetObjectFilePath(filePath); }
        }

        /// <summary>
        /// Deserializes an XML file to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="filePath">The XML file path.</param>
        public static T XmlFileToObject<T>(string filePath)
        {
            try
            {
                // Read file contents and deserialize the XML string to an object
                string xml = FileFunc.FileReadText(filePath);
                T value = XmlToObject<T>(xml);
                // Set the serialization-bound file
                if (value is IObjectSerializeFile objSerializeFile) { objSerializeFile.SetObjectFilePath(filePath); }
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"XmlFileToObject Error: {ex.Message}\nFileName: {filePath}", ex);
            }
        }

        /// <summary>
        /// Gets the JSON serializer settings.
        /// </summary>
        /// <param name="ignoreDefaultValue">Whether to ignore default values.</param>
        /// <param name="ignoreNullValue">Whether to ignore null values.</param>
        /// <param name="includeTypeName">Whether to include type names.</param>
        private static JsonSerializerSettings GetJsonSerializerSettings(bool ignoreDefaultValue, bool ignoreNullValue, bool includeTypeName)
        {
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            // Ignore default values
            if (ignoreDefaultValue)
                settings.DefaultValueHandling = DefaultValueHandling.Ignore;
            // Ignore null values
            if (ignoreNullValue)
                settings.NullValueHandling = NullValueHandling.Ignore;
            // Include type names
            if (includeTypeName)
            {
                settings.TypeNameHandling = TypeNameHandling.Auto;
                settings.SerializationBinder = new JsonSerializationBinder();
            }
            // Use string representation for enum types
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            return settings;
        }

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="ignoreDefaultValue">Whether to ignore default values.</param>
        /// <param name="ignoreNullValue">Whether to ignore null values.</param>
        /// <param name="includeTypeName">Whether to include type names.</param>
        public static string ObjectToJson(object value, bool ignoreDefaultValue = true, bool ignoreNullValue = true, bool includeTypeName = true)
        {
            // Pre-serialization operations
            DoBeforeSerialize(SerializeFormat.Json, value);

            // Serialize to JSON string
            var settings = GetJsonSerializerSettings(ignoreDefaultValue, ignoreNullValue, includeTypeName);
            string json = JsonConvert.SerializeObject(value, settings);

            // Post-serialization operations
            DoAfterSerialize(SerializeFormat.Json, value);
            return json;
        }

        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <param name="includeTypeName">Whether to include type names.</param>
        public static T JsonToObject<T>(string json, bool includeTypeName = true)
        {
            // Deserialize the JSON string
            var settings = GetJsonSerializerSettings(true, false, includeTypeName);
            object value = JsonConvert.DeserializeObject(json, typeof(T), settings);
            // Post-deserialization operations
            DoAfterDeserialize(SerializeFormat.Json, value);
            return (T)value;
        }

        /// <summary>
        /// Serializes an object to a JSON file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The JSON file path.</param>
        public static void ObjectToJsonFile(object value, string filePath)
        {
            string json = ObjectToJson(value, true);
            FileFunc.FileWriteText(filePath, json);
            // Set the serialization-bound file
            if (value is IObjectSerializeFile objectSerializeFile) { objectSerializeFile.SetObjectFilePath(filePath); }
        }

        /// <summary>
        /// Deserializes a JSON file to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="filePath">The JSON file path.</param>
        public static T JsonFileToObject<T>(string filePath)
        {
            try
            {
                string json = FileFunc.FileReadText(filePath);
                T value = JsonToObject<T>(json);
                // Set the serialization-bound file
                if (value is IObjectSerializeFile objectSerializeFile) { objectSerializeFile.SetObjectFilePath(filePath); }
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"JsonFileToObject Error: {ex.Message}\nFileName: {filePath}", ex);
            }
        }



    }
}
