using System.ComponentModel;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// 表示 API 傳遞的標準資料結構，支援序列化、壓縮與加密處理。
    /// </summary>
    public abstract class ApiPayload : IObjectSerialize
    {
        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            if (Value is IObjectSerialize objectSerialize)
            {
                BaseFunc.SetSerializeState(objectSerialize, serializeState);
            }
        }

        #endregion

        /// <summary>
        /// 傳遞資料的格式（原始、編碼或加密）。
        /// </summary>
        [JsonProperty("format")]
        public PayloadFormat Format { get; internal set; } = PayloadFormat.Plain;

        /// <summary>
        /// 傳遞資料。
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// 傳遞資料的型別名稱，用於反序列化還原時指定型別。
        /// </summary>
        [JsonProperty("type")]
        [DefaultValue("")]
        public string TypeName { get; set; } = string.Empty;
    }
}
