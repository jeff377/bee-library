namespace Bee.Define
{
    /// <summary>
    /// 套件發佈方式。序列化時以整數值表示（0/1），請勿變更既有成員的數值。
    /// </summary>
    public enum PackageDelivery : int
    {
        /// <summary>
        /// 回傳短時效 URL 供直接下載（建議用於大檔）。
        /// </summary>
        Url = 0,
        /// <summary>
        /// 由 API 直接傳回位元組內容（小檔或內部環境）。
        /// </summary>
        Api = 1
    }
}
