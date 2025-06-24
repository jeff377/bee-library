#nullable disable
using System.ComponentModel;
using Bee.Base;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 樹狀結構控制項。
    /// </summary>
    [ToolboxItem(true)]
    [Description("樹狀結構控制項")]
    public class BeeTreeView : System.Windows.Forms.TreeView
    {
        private BeePropertyGrid _propertyGrid = null;

        #region ObjectTreeNodeCreating 事件

        /// <summary>
        /// 建立物件樹狀節點前引發的事件。
        /// </summary>
        [Description("建立物件樹狀節點前引發的事件。")]
        [Category("ObjectTree")]
        public event ObjectTreeNodeCreatingEventHandler ObjectTreeNodeCreating;

        /// <summary>
        /// 引發 ObjectTreeNodeCreating 事件。
        /// </summary>
        public void OnObjectTreeNodeCreating(ObjectTreeNodeCreatingEventArgs e)
        {
            ObjectTreeNodeCreating?.Invoke(this, e);
        }

        #endregion

        #region ObjectTreeNodeCreated 事件

        /// <summary>
        /// 建立物件的樹狀節點事件。
        /// </summary>
        [Description("建立物件的樹狀節點事件。")]
        [Category("ObjectTree")]
        public event ObjectTreeNodeCreatedEventHandler ObjectTreeNodeCreated;

        /// <summary>
        /// 引發 ObjectTreeNodeCreated 事件。
        /// </summary>
        public void OnObjectTreeNodeCreated(ObjectTreeNodeCreatedEventArgs e)
        {
            ObjectTreeNodeCreated?.Invoke(this, e);
        }

        #endregion

        /// <summary>
        /// 繫結屬性視窗控制項，以顯示節點繫結物件。 
        /// </summary>
        [Description("繫結屬性視窗控制項，以顯示節點繫結物件。")]
        [DefaultValue((string)null)]
        public BeePropertyGrid PropertyGrid
        {
            get { return _propertyGrid; }
            set
            {
                if (_propertyGrid != value)
                {
                    // 移除舊的 PropertyValueChanged 事件的導向函式
                    if (_propertyGrid != null)
                        _propertyGrid.PropertyValueChanged -= PropertyGrid_PropertyValueChanged;

                    _propertyGrid = value;

                    // 加入新的 PropertyValueChanged 事件的導向函式
                    if (_propertyGrid != null)
                        _propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;
                }
            }
        }

        /// <summary>
        /// PropertyGrid 的 PropertyValueChanged 的事件處理方法。
        /// </summary>
        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // 節點與屬性視窗的繫結物件不一致時，則離開
            if (SelectedNode == null || SelectedNode.Tag != PropertyGrid.SelectedObject) { return; }

            // 取得 TreeNodeAttribute
            object value = this.PropertyGrid.SelectedObject;
            var attribute = (TreeNodeAttribute)BaseFunc.GetAttribute(value, typeof(TreeNodeAttribute));
            if (attribute == null) { return; }
            if (StrFunc.IsEmpty(attribute.PropertyName)) { return; }

            // 取得異動的屬性名稱
            string propertyName = BaseFunc.CStr(BaseFunc.GetPropertyValue(e.ChangedItem, "PropertyName"));
            // 若異動屬性為顯示相關屬性，則更新節點顯示文字
            string[] propertyNames = StrFunc.Split(attribute.PropertyName, ",");
            if (propertyNames.Contains(propertyName))
                SelectedNode.Text = TreeNodeAttribute.GetDisplayText(value);
        }

        /// <summary>
        /// 覆寫 OnAfterSelect 方法。
        /// </summary>
        protected override void OnAfterSelect(TreeViewEventArgs e)
        {
            base.OnAfterSelect(e);

            if (e.Node == null) { return; }
            if (e.Action == TreeViewAction.ByMouse || e.Action == TreeViewAction.ByKeyboard)
                ShowSelectedNode();
        }

        /// <summary>
        /// 在屬性視窗中顯示選取節點的繫結物件。
        /// </summary>
        private void ShowSelectedNode()
        {
            if (PropertyGrid == null || SelectedNode == null) { return; }
            PropertyGrid.SelectedObject = SelectedNode.Tag;
        }

        /// <summary>
        /// 建立物件的樹狀結構。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="useTagProperty">是否使用 ITagProperty.Tag 屬性去儲存樹狀結節。</param>
        /// <param name="isClear">是否清除所有節點。</param>
        public void BuildObjectTree(object value, bool useTagProperty = true, bool isClear = true)
        {
            this.BeginUpdate();
            var builder = new BeeTreeViewBuilder(this, useTagProperty);
            builder.BuildObjectTree(value, isClear);
            if (PropertyGrid != null)
            {
                PropertyGrid.SelectedObject = null;
            }
            if (this.Nodes.Count > 0)
            {
                // 預設展開第一階節點
                this.Nodes[0].Expand();
                this.SelectedNode = this.Nodes[0];
                ShowSelectedNode();
            }
            this.EndUpdate();
        }
    }
}
