using Bee.UI.Core;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// UI 相關的視窗 (View) 服務。
    /// </summary>
    public class TUIViewService : IUIViewService
    {
        /// <summary>
        /// 顯示連線設定。
        /// </summary>
        /// <returns>連線設定完成傳回 true，反之傳回 false。</returns>
        public bool ShowConnect()
        {
            var form = new frmConnect();
            return form.ShowDialog() == DialogResult.OK;
        }
    }
}
