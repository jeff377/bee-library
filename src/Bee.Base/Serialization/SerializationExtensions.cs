using System;
using Bee.Base;

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
            return SerializeFunc.ObjectToXml(value);
        }

        /// <summary>
        /// Serializes the object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        public static string ToJson(this IObjectSerializeBase value)
        {
            return SerializeFunc.ObjectToJson(value);
        }

        /// <summary>
        /// Serializes the object to an XML file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The XML file path.</param>
        public static void ToXmlFile(this IObjectSerializeFile value, string filePath)
        {
            SerializeFunc.ObjectToXmlFile(value, filePath);
        }

        /// <summary>
        /// Serializes the object to a JSON file.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="filePath">The JSON file path.</param>
        public static void ToJsonFile(this IObjectSerializeFile value, string filePath)
        {
            SerializeFunc.ObjectToJsonFile(value, filePath);
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

            sExtension = FileFunc.GetExtension(value.ObjectFilePath);
            if (StrFunc.IsEquals(sExtension, ".xml"))
                SerializeFunc.ObjectToXmlFile(value, value.ObjectFilePath);
            else if (StrFunc.IsEquals(sExtension, ".json"))
                SerializeFunc.ObjectToJsonFile(value, value.ObjectFilePath);
            else
                throw new NotSupportedException();
        }
    }
}
