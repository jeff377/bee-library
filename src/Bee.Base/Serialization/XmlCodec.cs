namespace Bee.Base.Serialization
{
    /// <summary>
    /// XML serialization codec. Round-trips objects via <see cref="System.Xml.Serialization.XmlSerializer"/>
    /// and dispatches lifecycle hooks for objects implementing <see cref="IObjectSerialize"/> /
    /// <see cref="IObjectSerializeProcess"/>.
    /// </summary>
    public static class XmlCodec
    {
        /// <summary>
        /// Serializes an object to an XML string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        public static string Serialize(object value)
        {
            if (value == null)
                return string.Empty;

            // Pre-serialization operations
            SerializationLifecycle.NotifyBefore(SerializeFormat.Xml, value);

            // Serialize and write to string
            string xml = string.Empty;
            using (Utf8StringWriter writer = new Utf8StringWriter())
            {
                var serializer = XmlSerializerCache.Get(value.GetType());
                serializer.Serialize(writer, value);
                xml = writer.ToString();
            }

            // Post-serialization operations
            SerializationLifecycle.NotifyAfter(SerializeFormat.Xml, value);
            return xml;
        }

        /// <summary>
        /// Deserializes an XML string to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="xml">The XML string.</param>
        public static T? Deserialize<T>(string xml)
        {
            return (T?)Deserialize(xml, typeof(T));
        }

        /// <summary>
        /// Deserializes an XML string to an object.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <param name="type">The object type.</param>
        public static object? Deserialize(string xml, Type type)
        {
            if (StrFunc.IsEmpty(xml))
                return default;

            object? value;
            using (StringReader reader = new StringReader(xml))
            {
                var serializer = XmlSerializerCache.Get(type);
                value = serializer.Deserialize(reader);
            }

            // Post-deserialization operations
            SerializationLifecycle.NotifyAfterDeserialize(SerializeFormat.Xml, value);
            return value;
        }

        /// <summary>
        /// Serializes an object to an XML file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The XML file path.</param>
        public static void SerializeToFile(object value, string filePath)
        {
            string xml = Serialize(value);
            FileFunc.FileWriteText(filePath, xml);
            // Set the serialization-bound file
            if (value is IObjectSerializeFile fileSerialize) { fileSerialize.SetObjectFilePath(filePath); }
        }

        /// <summary>
        /// Deserializes an XML file to an object.
        /// </summary>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <param name="filePath">The XML file path.</param>
        public static T? DeserializeFromFile<T>(string filePath)
        {
            try
            {
                // Read file contents and deserialize the XML string to an object
                string xml = FileFunc.FileReadText(filePath);
                T? value = Deserialize<T>(xml);
                // Set the serialization-bound file
                if (value is IObjectSerializeFile objSerializeFile) { objSerializeFile.SetObjectFilePath(filePath); }
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"DeserializeFromFile Error: {ex.Message}\nFileName: {filePath}", ex);
            }
        }
    }
}
