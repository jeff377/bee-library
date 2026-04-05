using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// 取得單一套件（ZIP）的引數；以 AppId + ComponentId + Version + Platform + Channel 精準定位。
    /// </summary>
    [MessagePackObject]
    public sealed class GetPackageArgs : BusinessArgs
    {
        /// <summary>
        /// 應用程式/工具代號。
        /// </summary>
        [Key(100)]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// 元件代號（例：Main、Reference、Plugin-XYZ）。
        /// </summary>
        [Key(101)]
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// 要下載的版本字串（例：1.2.4）。
        /// </summary>
        [Key(102)]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 執行平台（例：Win-x64、Win-arm64、macOS）。
        /// </summary>
        [Key(103)]
        public string Platform { get; set; } = "Win-x64";

        /// <summary>
        /// 發佈通道（例：Stable、Beta）。
        /// </summary>
        [Key(104)]
        public string Channel { get; set; } = "Stable";

        /// <summary>
        /// （選用）同版多變體時的檔案識別（檔名/儲存 Key/變體代碼）。
        /// </summary>
        [Key(105)]
        public string FileId { get; set; } = string.Empty;
    }
}
