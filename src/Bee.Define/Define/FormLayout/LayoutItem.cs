using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 排版項目。
    /// </summary>
    [Serializable]
    [XmlType("LayoutItem")]
    [Description("排版項目。")]
    [TreeNode]
    public class LayoutItem : LayoutItemBase
    {
        private int _rowSpan = 1;
        private int _columnSpan = 1;
        private ListItemCollection _listItems = null;
        private PropertyCollection _extendedProperties = null;

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("欄位名稱。")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 標題文字。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("標題文字。")]
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// 控制項類型。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("控制項類型。")]
        public ControlType ControlType { get; set; } = ControlType.TextEdit;

        /// <summary>
        /// 合併列數。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("合併列數。")]
        [DefaultValue(1)]
        public int RowSpan
        {
            get { return _rowSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _rowSpan = value;
            }
        }

        /// <summary>
        /// 合併欄數。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("合併欄數。")]
        [DefaultValue(1)]
        public int ColumnSpan
        {
            get { return _columnSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _columnSpan = value;
            }
        }

        /// <summary>
        /// 關連程式代碼。
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("關連程式代碼。")]
        [DefaultValue("")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// 是否唯讀。
        /// </summary>
        [Category(PropertyCategories.Appearance)]
        [XmlAttribute]
        [Description("是否唯讀。")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// 顯示格式化。
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("顯示格式化。")]
        [DefaultValue("")]
        public string DisplayFormat { get; set; } = string.Empty;

        /// <summary>
        /// 數值格式化。
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("數值格式化。")]
        [DefaultValue("")]
        public string NumberFormat { get; set; } = string.Empty;

        /// <summary>
        /// 清單項目集合。
        /// </summary>
        [Category(PropertyCategories.Data)]
        [Description("清單項目集合。")]
        [DefaultValue(null)]
        public ListItemCollection ListItems
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _listItems)) { return null; }
                if (_listItems == null) { _listItems = new ListItemCollection(); }
                return _listItems;
            }
        }

        /// <summary>
        /// 延伸屬性集合。
        /// </summary>
        [Description("延伸屬性集合。")]
        [DefaultValue(null)]
        public PropertyCollection ExtendedProperties
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _extendedProperties)) { return null; }
                if (_extendedProperties == null) { _extendedProperties = new PropertyCollection(); }
                return _extendedProperties;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_listItems, serializeState);
            BaseFunc.SetSerializeState(_extendedProperties, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.FieldName} - {this.Caption}";
        }
    }
}
