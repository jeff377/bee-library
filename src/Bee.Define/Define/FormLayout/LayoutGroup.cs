using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 排版群組。
    /// </summary>
    [Serializable]
    [XmlType("LayoutGroup")]
    [Description("排版群組。")]
    [TreeNode]
    public class LayoutGroup : CollectionItem
    {
        private int _columnCount = 1;
        private LayoutItemCollection _items = null;

        /// <summary>
        /// 群組名稱。
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("群組名稱。")]
        [DefaultValue("")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 標題文字。
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("標題文字。")]
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// 是否顯示標題。
        /// </summary>
        [XmlAttribute]
        [Description("是否顯示標題。")]
        [DefaultValue(true)]
        public bool ShowCaption { get; set; } = true;

        /// <summary>
        /// 欄位數。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("欄位數。")]
        public int ColumnCount
        {
            get { return _columnCount; }
            set
            {
                if (value < 1) { value = 1; }
                _columnCount = value;
            }
        }

        /// <summary>
        /// 佈局項目集合。
        /// </summary>
        [Description("佈局項目集合。")]
        [Browsable(false)]
        [XmlArrayItem(typeof(LayoutItem))]
        [XmlArrayItem(typeof(LayoutGrid))]
        [DefaultValue(null)]
        public LayoutItemCollection Items
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _items)) { return null; }
                if (_items == null) { _items = new LayoutItemCollection(); }
                return _items;
            }
        }

        /// <summary>
        /// 尋找指定資料表格排版。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public LayoutGrid FindGrid(string tableName)
        {
            foreach (LayoutItemBase item in this.Items)
            {
                if (item is LayoutGrid grid)
                {
                    if (StrFunc.IsEquals(grid.TableName, tableName))
                        return grid;
                }
            }
            return null;
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_items, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.Name} - {this.Caption}";
        }
    }
}
