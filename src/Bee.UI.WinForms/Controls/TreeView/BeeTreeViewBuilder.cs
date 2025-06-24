#nullable disable
using System.Collections;
using System.Reflection;
using Bee.Base;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// TreeView 樹狀結構控制項產生器。
    /// </summary>
    internal class BeeTreeViewBuilder
    {
        private readonly BeeTreeView _TreeView;
        private readonly ImageList _ImageList;
        private bool _UseTagProperty = true;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="treeView">樹狀結構控制項。</param>
        /// <param name="useTagProperty">是否使用 ITagProperty.Tag 屬性去儲存樹狀結節。。</param>
        public BeeTreeViewBuilder(BeeTreeView treeView, bool useTagProperty)
        {
            _TreeView = treeView;
            _ImageList = treeView.ImageList;
            _UseTagProperty = useTagProperty;
        }

        #endregion

        /// <summary>
        /// 樹狀結構控制項。
        /// </summary>
        public BeeTreeView TreeView
        {
            get { return _TreeView; }
        }

        /// <summary>
        /// 影像集合。
        /// </summary>
        public ImageList ImageList
        {
            get { return _ImageList; }
        }

        /// <summary>
        /// 是否使用 ITagProperty.Tag 屬性去儲存樹狀結節。
        /// </summary>
        public bool UseTagProperty
        {
            get { return _UseTagProperty; }
        }

        /// <summary>
        /// 建立物件的樹狀結構。
        /// </summary>
        /// <param name="value">繫結的物件。</param>
        /// <param name="isClear">是否清除所有節點。</param>
        public void BuildObjectTree(object value, bool isClear)
        {
            // 清除所有節點
            if (isClear) { this.TreeView.Nodes.Clear(); }
            // 建立物件的樹狀結構
            BuildObjectTree(null, value);
            // 繫結物件會儲存於 Tag 屬性
            this.TreeView.Tag = value;
        }

        /// <summary>
        /// 建立物件的樹狀結構。
        /// </summary>
        /// <param name="parentNode">父結節。</param>
        /// <param name="value">物件。</param>
        private void BuildObjectTree(TreeNode parentNode, object value)
        {
            TreeNode oNode;
            object oPropertyValue;
            IEnumerator oEnumerator;
            bool bCancel;

            // 建立節點
            oNode = CreateNode(value, parentNode, out bCancel);
            if (bCancel) { return; } // 若物件不具有 TreeNodeAttribute 則離開

            // 集合若未建立節點，用上一層傳入的結節為父節點
            if ((value is ICollectionBase || value is IKeyCollectionBase) && (oNode == null))
                oNode = parentNode;

            // 集合屬性列舉成員後則離開
            if (value is ICollectionBase || value is IKeyCollectionBase)
            {
                oEnumerator = (value as IEnumerable).GetEnumerator();
                while (oEnumerator.MoveNext())
                {
                    // 集合成員使用遞迴呼叫建立集合的子節點
                    oPropertyValue = oEnumerator.Current;
                    BuildObjectTree(oNode, oPropertyValue);
                }
                return;
            }

            // 建立物件的屬性樹狀結構
            BuildPropertyTree(oNode, value);
        }

        /// <summary>
        /// 建立物件的屬性樹狀結構。
        /// </summary>
        /// <param name="parentNode">父結節。</param>
        /// <param name="value">物件。</param>
        private void BuildPropertyTree(TreeNode parentNode, object value)
        {
            MemberInfo[] oMembers;
            object[] oAttributes;
            object oPropertyValue;

            if (parentNode == null || value == null) { return; }

            // 判斷屬性成員是否有子節點
            oMembers = value.GetType().GetMembers();
            foreach (var member in oMembers)
            {
                if (member.MemberType == MemberTypes.Property)
                {
                    oAttributes = member.GetCustomAttributes(typeof(TreeNodeIgnoreAttribute), true);
                    // 屬性有套用 TreeNodeIgnoreAttribute 則往下一個成員
                    if (oAttributes.Length > 0) { continue; }

                    //屬性使用遞迴呼叫建立子節點 
                    oPropertyValue = BaseFunc.GetPropertyValue(value, member.Name);
                    BuildObjectTree(parentNode, oPropertyValue);
                }
            }
        }

        /// <summary>
        /// 建立物件的樹狀節點。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="parentNode">父節點</param>
        /// <param name="cancel">是否取消建立節點的動作。</param>
        private TreeNode CreateNode(object value, TreeNode parentNode, out bool cancel)
        {
            cancel = false;

            //若類別未套用 TreeNodeAttribute 則傳回 null
            var attribute = (TreeNodeAttribute)BaseFunc.GetAttribute(value, typeof(TreeNodeAttribute));
            if (attribute == null)
            {
                cancel = true;
                return null;
            }

            // 若集合類別設定不建立節點則傳回 null
            if ((value is ICollectionBase || value is IKeyCollectionBase) && (!attribute.CollectionFolder)) { return null; }

            // 觸發 ObjectTreeNodeCreating 事件
            if (!this.RaiseObjectTreeNodeCreatingEvent(value))
            {
                cancel = true;
                return null;
            }

            // 建立節點
            string text = TreeNodeAttribute.GetDisplayText(value);
            TreeNode node;
            if (parentNode == null)
                node = this.TreeView.Nodes.Add(text);
            else
                node = parentNode.Nodes.Add(text);

            int imageIndex = GetImageIndex(value.GetType().Name);
            if (imageIndex >= 0)
            {
                node.ImageIndex = imageIndex;
                node.SelectedImageIndex = imageIndex;
            }

            // 節點的 Tag 記錄物件，物件的 Tag 記錄節點
            node.Tag = value;
            if (value is ITagProperty && this.UseTagProperty)
                (value as ITagProperty).Tag = node;

            // 觸發 ObjectTreeNodeCreated 事件
            RaiseObjectTreeNodeCreatedEvent(node);

            return node;
        }

        /// <summary>
        /// 觸發 ObjectTreeNodeCreating 事件。 
        /// </summary>
        /// <param name="value">節點繫結的資料。</param>
        /// <remarks>當事件被取消時會傳回 false，反之傳回 true。 </remarks>
        private bool RaiseObjectTreeNodeCreatingEvent(object value)
        {
            var args = new ObjectTreeNodeCreatingEventArgs();
            args.Value = value;
            this.TreeView.OnObjectTreeNodeCreating(args);
            return !args.Cancel;
        }

        /// <summary>
        /// 觸發 ObjectTreeNodeCreated 事件。 
        /// </summary>
        /// <param name="node">建立的樹狀節點。</param>
        private void RaiseObjectTreeNodeCreatedEvent(TreeNode node)
        {
            var args = new ObjectTreeNodeCreatedEventArgs();
            args.Node = node;
            this.TreeView.OnObjectTreeNodeCreated(args);
        }

        /// <summary>
        /// 取得圖示索引值。
        /// </summary>
        /// <param name="key">圖示鍵值。</param>
        private int GetImageIndex(string key)
        {
            if (this.ImageList == null) { return -1; }

            for (int N1 = 0; N1 < this.ImageList.Images.Count; N1++)
            {
                if (this.ImageList.Images.Keys[N1] == key)
                    return N1;
            }
            return -1;
        }
    }
}
