using System;
using System.ComponentModel;

namespace Bee.Base
{
    /// <summary>
    /// ApiConnector 模組的記錄選項。
    /// </summary>
    [Serializable]
    [Description("ApiConnector 模組的記錄選項。")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ApiConnectorLogOptions
    {
        /// <summary>
        /// 是否記錄 JSON-RPC 的原始資料內容（params.value 與 result.value，尚未序列化/壓縮/加密）。
        /// </summary>
        [Description("是否記錄 JSON-RPC 的原始資料內容（params.value 與 result.value，尚未序列化/壓縮/加密）。")]
        public bool RawData { get; set; }

        /// <summary>
        /// 是否記錄 JSON-RPC 的編碼後資料（經序列化/壓縮/加密處理的二進位內容）。
        /// </summary>
        [Description("是否記錄 JSON-RPC 的編碼後資料（經序列化/壓縮/加密處理的二進位內容）。")]
        public bool EncodedData { get; set; }
    }
}
