using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// Provides API payload handling options, such as serialization, compression, and encryption.
    /// </summary>
    [Serializable]
    [XmlType("ApiPayloadOptions")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Description("Provides API payload handling options, such as serialization, compression, and encryption.")]
    public class ApiPayloadOptions
    {
        /// <summary>
        /// Specifies the serializer name, e.g., messagepack, binaryformatter.
        /// </summary>
        [Description("Specifies the serializer name, e.g., messagepack, binaryformatter.")]
        public string Serializer { get; set; } = "messagepack";

        /// <summary>
        /// Specifies the compressor name, e.g., gzip, none.
        /// </summary>
        [Description("Specifies the compressor name, e.g., gzip, none.")]
        public string Compressor { get; set; } = "gzip";

        /// <summary>
        /// Specifies the encryptor name, e.g., aes-cbc-hmac, none.
        /// </summary>
        [Description("Specifies the encryptor name, e.g., aes-cbc-hmac, none.")]
        public string Encryptor { get; set; } = "aes-cbc-hmac";

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return $"Serializer: {Serializer}, Compressor: {Compressor}, Encryptor: {Encryptor}";
        }
    }

}
