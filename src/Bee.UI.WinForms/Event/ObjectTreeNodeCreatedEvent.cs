namespace Bee.UI.WinForms
{
    /// <summary>
    /// 建立物件的樹狀節點的事件引數。
    /// </summary>
    public class ObjectTreeNodeCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// 建立的樹狀節點。
        /// </summary>
        public TreeNode? Node { get; set; } = null;
    }
}
