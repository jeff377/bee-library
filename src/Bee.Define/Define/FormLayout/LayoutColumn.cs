using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料表格排版欄位。
    /// </summary>
    [Serializable]
    [XmlType("LayoutColumn")]
    [Description("資料表格排版欄位。")]
    [TreeNode]
    public class LayoutColumn : CollectionItem
    {
        private ListItemCollection _listItems = null;
        private PropertyCollection _extendedProperties = null;

        /// <summary>
        /// 建構函式。
        /// </summary>
        public LayoutColumn()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="controlType">控制項類型。</param>
        public LayoutColumn(string fieldName, string caption, ColumnControlType controlType)
        {
            FieldName = fieldName;
            Caption = caption;
            ControlType = controlType;
        }

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
        public ColumnControlType ControlType { get; set; } = ColumnControlType.TextEdit;

        /// <summary>
        /// 關連程式代碼。
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("關連程式代碼。")]
        [DefaultValue("")]
        public string ProgId { get; set; }

        /// <summary>
        /// 是否顯示。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("是否顯示。")]
        [DefaultValue(true)]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 是否唯讀。
        /// </summary>
        [Category(PropertyCategories.Appearance)]
        [XmlAttribute]
        [Description("是否唯讀。")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// 欄寬，設定值大於 0 才有效。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("欄寬，設定值大於 0 才有效。")]
        [DefaultValue(0)]
        public int Width { get; set; } = 0;

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
