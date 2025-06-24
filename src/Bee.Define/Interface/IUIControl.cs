namespace Bee.Define
{
    /// <summary>
    /// UI 控制項介面。
    /// </summary>
    public interface IUIControl
    {
        /// <summary>
        /// 依表單模式設定控制項狀態。
        /// </summary>
        /// <param name="formMode">單筆資料表單模式。</param>
        void SetControlState(SingleFormMode formMode);
    }
}
