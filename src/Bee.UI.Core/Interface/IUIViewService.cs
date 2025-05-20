namespace Bee.UI.Core
{
    /// <summary>
    ///  UI 相關的視窗 (View) 服務。
    /// </summary>
    public interface IUIViewService
    {
        /// <summary>
        /// 顯示連線設定。
        /// </summary>
        /// <returns>連線設定完成傳回 true，反之傳回 false。</returns>
        bool ShowConnect();
    }
}
