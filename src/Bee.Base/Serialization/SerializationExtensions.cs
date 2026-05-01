namespace Bee.Base.Serialization
{
    /// <summary>
    /// Extension methods for serialization.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        /// Serializes the object to an XML string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        public static string ToXml(this IObjectSerializeBase value)
        {
            return XmlCodec.Serialize(value);
        }

        /// <summary>
        /// Serializes the object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        public static string ToJson(this IObjectSerializeBase value)
        {
            return JsonCodec.Serialize(value);
        }

        /// <summary>
        /// Serializes the object to an XML file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The XML file path.</param>
        public static void ToXmlFile(this IObjectSerializeFile value, string filePath)
        {
            XmlCodec.SerializeToFile(value, filePath);
        }

        /// <summary>
        /// Serializes the object to a JSON file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The JSON file path.</param>
        public static void ToJsonFile(this IObjectSerializeFile value, string filePath)
        {
            JsonCodec.SerializeToFile(value, filePath);
        }

        /// <summary>
        /// Saves the object to its bound file path.
        /// </summary>
        /// <param name="value">The object to save.</param>
        public static void Save(this IObjectSerializeFile value)
        {
            string sExtension;

            if (StrFunc.IsEmpty(value.ObjectFilePath))
                throw new ArgumentException("ObjectFilePath is empty");

            sExtension = Path.GetExtension(value.ObjectFilePath);
            if (StrFunc.IsEquals(sExtension, ".xml"))
                XmlCodec.SerializeToFile(value, value.ObjectFilePath);
            else if (StrFunc.IsEquals(sExtension, ".json"))
                JsonCodec.SerializeToFile(value, value.ObjectFilePath);
            else
                throw new NotSupportedException();
        }
    }
}
