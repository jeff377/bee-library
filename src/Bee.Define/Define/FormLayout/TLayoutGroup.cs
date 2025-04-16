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
    public class TLayoutGroup : TCollectionItem
    {
        private string _Name = string.Empty;
        private string _Caption = string.Empty;
        private bool _ShowCaption = true;
        private int _ColumnCount = 1;
        private TLayoutItemCollection _Items = null;

        /// <summary>
        /// 群組名稱。
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("群組名稱。")]
        [DefaultValue("")]
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        /// <summary>
        /// 標題文字。
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("標題文字。")]
        [DefaultValue("")]
        public string Caption
        {
            get { return _Caption; }
            set { _Caption = value; }
        }

        /// <summary>
        /// 是否顯示標題。
        /// </summary>
        [XmlAttribute]
        [Description("是否顯示標題。")]
        [DefaultValue(true)]
        public bool ShowCaption
        {
            get { return this._ShowCaption; }
            set { this._ShowCaption = value; }
        }

        /// <summary>
        /// 欄位數。
        /// </summary>
        [Category(Category.Layout)]
        [XmlAttribute]
        [Description("欄位數。")]
        public int ColumnCount
        {
            get { return _ColumnCount; }
            set
            {
                if (value < 1) { value = 1; }
                _ColumnCount = value;
            }
        }

        /// <summary>
        /// 佈局項目集合。
        /// </summary>
        [Description("佈局項目集合。")]
        [Browsable(false)]
        [XmlArrayItem(typeof(TLayoutItem))]
        [XmlArrayItem(typeof(TLayoutGrid))]
        [DefaultValue(null)]
        public TLayoutItemCollection Items
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Items)) { return null; }
                if (_Items == null) { _Items = new TLayoutItemCollection(); }
                return _Items;
            }
        }

        /// <summary>
        /// 尋找指定資料表格排版。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public TLayoutGrid FindGrid(string tableName)
        {
            foreach (TLayoutItemBase item in this.Items)
            {
                if (item is TLayoutGrid grid)
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
        public override void SetSerializeState(ESerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_Items, serializeState);
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
