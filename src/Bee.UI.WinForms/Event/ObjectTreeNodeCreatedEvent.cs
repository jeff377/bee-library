namespace Bee.UI.WinForms
{
    /// <summary>
    /// ObjectTreeNodeCreated 事件委派宣告，建立物件樹狀節點後引發的事件。
    /// </summary>
    public delegate void ObjectTreeNodeCreatedEventHandler(object sender, ObjectTreeNodeCreatedEventArgs e);

    /// <summary>
    /// ObjectTreeNodeCreated 事件引數。
    /// </summary>
    public class ObjectTreeNodeCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// 建立的樹狀節點。
        /// </summary>
        public TreeNode? Node { get; set; } = null;
    }
}
