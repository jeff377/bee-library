namespace Bee.Api.Core
{
    /// <summary>
    /// 定義可編碼與解碼的 JSON-RPC 負載資料結構。
    /// </summary>
    public interface IEncodablePayload
    {
        /// <summary>
        /// 傳遞資料。
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// 傳遞資料的型別名稱，用於反序列化還原時指定型別。
        /// </summary>
        string TypeName { get; set; }

        /// <summary>
        /// 資料是否已經進行編碼（例如加密或壓縮）。
        /// </summary>
        bool IsEncoded { get; }

        /// <summary>
        /// 將傳遞資料進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        void Encode();

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        void Decode();
    }

}
