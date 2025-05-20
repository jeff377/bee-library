namespace Bee.UI.WinForms
{
    /// <summary>
    /// ObjectTreeNodeCreating 事件委派宣告，建立物件樹狀節點前引發的事件。
    /// </summary>
    public delegate void ObjectTreeNodeCreatingEventHandler(object sender, ObjectTreeNodeCreatingEventArgs e);

    /// <summary>
    /// ObjectTreeNodeCreating 事件引數。
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
