using System;
using System.ComponentModel;

namespace Bee.Base
{
    /// <summary>
    /// Logging options for the ApiConnector module.
    /// </summary>
    [Serializable]
    [Description("Logging options for the ApiConnector module.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ApiConnectorLogOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ApiConnectorLogOptions()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rawData">Whether to log the raw JSON-RPC data content.</param>
        /// <param name="encodedData">Whether to log the encoded JSON-RPC data.</param>
        public ApiConnectorLogOptions(bool rawData, bool encodedData)
        {
            RawData = rawData;
            EncodedData = encodedData;
        }

        /// <summary>
        /// Whether to log the raw JSON-RPC data content (params.value and result.value, before serialization/compression/encryption).
        /// </summary>
        [Description("Whether to log the raw JSON-RPC data content (params.value and result.value, before serialization/compression/encryption).")]
        public bool RawData { get; set; }

        /// <summary>
        /// Whether to log the encoded JSON-RPC data (binary content after serialization/compression/encryption).
        /// </summary>
        [Description("Whether to log the encoded JSON-RPC data (binary content after serialization/compression/encryption).")]
        public bool EncodedData { get; set; }

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
