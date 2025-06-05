namespace Bee.UI.Core
{
    /// <summary>
    /// 服務端點儲存介面。
    /// </summary>
    public interface IEndpointStorage
    {
        /// <summary>
        /// 儲存服務端點。
        /// </summary>
        void SaveEndpoint(string endpoint);

        /// <summary>
        /// 取得服務端點。
        /// </summary>
        string LoadEndpoint();
    }

}
