using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// 取得單一套件（ZIP）的回傳結果。
    /// </summary>
    [MessagePackObject]
    public sealed class GetPackageResult : BusinessResult
    {
        /// <summary>
        /// 檔名（例：client-main-win-x64-1.2.4.zip）。
        /// </summary>
        [Key(100)]
        public string FileName { get; set; } = "package.zip";

        /// <summary>
        /// ZIP 檔位元組內容（Delivery = Api 時使用；大檔建議改以 URL 下載）。
        /// </summary>
        [Key(101)]
        public byte[] Content { get; set; } = new byte[0];

        /// <summary>
        /// 檔案大小（位元組）。
        /// </summary>
        [Key(102)]
        public long FileSize { get; set; }

        /// <summary>
        /// 檔案 SHA256（十六進位字串），供下載後完整性驗證。
        /// </summary>
        [Key(103)]
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// 短時效下載連結（Delivery = Url 時使用）。
        /// </summary>
        [Key(104)]
        public string PackageUrl { get; set; } = string.Empty;
    }
}
