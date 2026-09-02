using System.ComponentModel;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Provides API payload handling options: compression and encryption.
    /// </summary>
    /// <remarks>
    /// The body codec is deliberately absent. Compression and encryption are deployment policy —
    /// whether payloads are protected, and how — but which codec a body is written in is the
    /// client's own capability, so it is declared per request on the payload envelope instead. A
    /// request that declares none is read as MessagePack, which is what every client predating
    /// negotiation sends.
    /// </remarks>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Description("Provides API payload handling options: compression and encryption.")]
    public class ApiPayloadOptions
    {
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
            return $"Compressor: {Compressor}, Encryptor: {Encryptor}";
        }
    }

}
