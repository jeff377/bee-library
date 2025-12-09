namespace Bee.UI.WinForms
{
    /// <summary>
    /// 建立物件樹狀節點前的事件引數。
    /// </summary>
    public class ObjectTreeNodeCreatingEventArgs : EventArgs
    {
        /// <summary>
        /// 節點繫結的資料。
        /// </summary>
        public object? Value { get; set; } = null;

        /// <summary>
        /// 取消動作。
        /// </summary>
        public bool Cancel { get; set; }
    }
}
