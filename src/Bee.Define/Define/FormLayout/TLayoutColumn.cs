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
    public class TLayoutColumn : TCollectionItem
    {
        private string _FieldName = string.Empty;
        private string _Caption = string.Empty;
        private EColumnControlType _ControlType = EColumnControlType.TextEdit;
        private string _ProgID = string.Empty;
        private bool _Visible = true;
        private bool _ReadOnly = false;
        private int _Width = 0;
        private string _DisplayFormat = string.Empty;
        private string _NumberFormat = string.Empty;
        private TListItemCollection _ListItems = null;
        private TPropertyCollection _ExtendedProperties = null;

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TLayoutColumn()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="controlType">控制項類型。</param>
        public TLayoutColumn(string fieldName, string caption, EColumnControlType controlType)
        {
            _FieldName = fieldName;
            _Caption = caption;
            _ControlType = controlType;
        }

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
        public EColumnControlType ControlType
        {
            get { return _ControlType; }
            set { _ControlType = value; }
        }

        /// <summary>
        /// 關連程式代碼。
        /// </summary>
        [Category(Category.Data)]
        [XmlAttribute]
        [Description("關連程式代碼。")]
        [DefaultValue("")]
        public string ProgID
        {
            get { return _ProgID; }
            set { _ProgID = value; }
        }

        /// <summary>
        /// 是否顯示。
        /// </summary>
        [Category(Category.Layout)]
        [XmlAttribute]
        [Description("是否顯示。")]
        [DefaultValue(true)]
        public bool Visible
        {
            get { return _Visible; }
            set { _Visible = value; }
        }

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
        /// 欄寬，設定值大於 0 才有效。
        /// </summary>
        [Category(Category.Layout)]
        [XmlAttribute]
        [Description("欄寬，設定值大於 0 才有效。")]
        [DefaultValue(0)]
        public int Width
        {
            get { return _Width; }
            set { _Width = value; }
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
        public TListItemCollection ListItems
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _ListItems)) { return null; }
                if (_ListItems == null) { _ListItems = new TListItemCollection(); }
                return _ListItems;
            }
        }

        /// <summary>
        /// 延伸屬性集合。
        /// </summary>
        [Description("延伸屬性集合。")]
        [DefaultValue(null)]
        public TPropertyCollection ExtendedProperties
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _ExtendedProperties)) { return null; }
                if (_ExtendedProperties == null) { _ExtendedProperties = new TPropertyCollection(); }
                return _ExtendedProperties;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(ESerializeState serializeState)
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
