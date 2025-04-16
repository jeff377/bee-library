namespace Bee.Base
{
    /// <summary>
    /// 物件支援序列化至檔案的介面。
    /// </summary>
    public interface IObjectSerializeFile : IObjectSerialize
    {
        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        string ObjectFilePath { get; }

        /// <summary>
        /// 設定序列化繫結檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        void SetObjectFilePath(string filePath);
    }
}
