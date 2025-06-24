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
        private string _FieldName = string.Empty;
        private string _Caption = string.Empty;
        private ControlType _ControlType = ControlType.TextEdit;
        private int _RowSpan = 1;
        private int _ColumnSpan = 1;
        private bool _ReadOnly = false;
        private string _DisplayFormat = string.Empty;
        private string _NumberFormat = string.Empty;
        private ListItemCollection _ListItems = null;
        private PropertyCollection _ExtendedProperties = null;

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [Category(Category.Data)]
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("欄位名稱。")]
        public string FieldName
        {
            get { return _FieldName; }
            set { _FieldName = value; }
        }

        /// <summary>
        /// 標題文字。
        /// </summary>
        [Category(Category.Layout)]
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
        /// 控制項類型。
        /// </summary>
        [Category(Category.Layout)]
        [XmlAttribute]
        [Description("控制項類型。")]
        public ControlType ControlType
        {
            get { return _ControlType; }
            set { _ControlType = value; }
        }

        /// <summary>
        /// 合併列數。
        /// </summary>
        [Category(Category.Layout)]
        [XmlAttribute]
        [Description("合併列數。")]
        [DefaultValue(1)]
        public int RowSpan
        {
            get { return _RowSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _RowSpan = value;
            }
        }

        /// <summary>
        /// 合併欄數。
        /// </summary>
        [Category(Category.Layout)]
        [XmlAttribute]
        [Description("合併欄數。")]
        [DefaultValue(1)]
        public int ColumnSpan
        {
            get { return _ColumnSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _ColumnSpan = value;
            }
        }

        /// <summary>
        /// 關連程式代碼。
        /// </summary>
        [Category(Category.Data)]
        [XmlAttribute]
        [Description("關連程式代碼。")]
        [DefaultValue("")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// 是否唯讀。
        /// </summary>
        [Category(Category.Appearance)]
        [XmlAttribute]
        [Description("是否唯讀。")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return _ReadOnly; }
            set { _ReadOnly = value; }
        }

        /// <summary>
        /// 顯示格式化。
        /// </summary>
        [Category(Category.Data)]
        [XmlAttribute]
        [Description("顯示格式化。")]
        [DefaultValue("")]
        public string DisplayFormat
        {
            get { return _DisplayFormat; }
            set { _DisplayFormat = value; }
        }

        /// <summary>
        /// 數值格式化。
        /// </summary>
        [Category(Category.Data)]
        [XmlAttribute]
        [Description("數值格式化。")]
        [DefaultValue("")]
        public string NumberFormat
        {
            get { return _NumberFormat; }
            set { _NumberFormat = value; }
        }

        /// <summary>
        /// 清單項目集合。
        /// </summary>
        [Category(Category.Data)]
        [Description("清單項目集合。")]
        [DefaultValue(null)]
        public ListItemCollection ListItems
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _ListItems)) { return null; }
                if (_ListItems == null) { _ListItems = new ListItemCollection(); }
                return _ListItems;
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
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _ExtendedProperties)) { return null; }
                if (_ExtendedProperties == null) { _ExtendedProperties = new PropertyCollection(); }
                return _ExtendedProperties;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_ListItems, serializeState);
            BaseFunc.SetSerializeState(_ExtendedProperties, serializeState);
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
