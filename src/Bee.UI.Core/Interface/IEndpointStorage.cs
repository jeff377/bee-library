namespace Bee.UI.Core
{
    /// <summary>
    /// 服務端點儲存介面。
    /// </summary>
    public interface IEndpointStorage
    {
        /// <summary>
        /// 取得服務端點。
        /// </summary>
        string LoadEndpoint();

        /// <summary>
        /// 設定服務端點。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        void SetEndpoint(string endpoint);

        /// <summary>
        /// 設定並儲存服務端點。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        void SaveEndpoint(string endpoint);
    }

}
