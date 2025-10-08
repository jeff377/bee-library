using Bee.Base;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// 表單欄位。
    /// </summary>
    [Serializable]
    [XmlType("FormField")]
    [Description("表單欄位。")]
    [TreeNode]
    public class FormField : KeyCollectionItem
    {
        private FieldMappingCollection _relationFieldMappings = null;
        private FieldMappingCollection _lookupFieldMappings = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FormField()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="dbType">欄位資料型別。</param>
        public FormField(string fieldName, string caption, FieldDbType dbType)
        {
            this.FieldName = fieldName;
            Caption = caption;
            DbType = dbType;
        }

        #endregion

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("欄位名稱。")]
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 標題文字。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("標題文字。")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// 資料型別。
        /// </summary>
        [XmlAttribute]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        [Category(PropertyCategories.Data)]
        [Description("資料型別。")]
        public FieldDbType DbType { get; set; } = FieldDbType.String;

        /// <summary>
        /// 欄位類型。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("欄位類型。")]
        [DefaultValue(FieldType.DbField)]
        public FieldType Type { get; set; } = FieldType.DbField;

        /// <summary>
        /// 控制項類型。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Layout)]
        [Description("控制項類型。")]
        public ControlType ControlType { get; set; } = ControlType.TextEdit;

        /// <summary>
        /// 字串最大長度。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("字串最大長度。")]
        [DefaultValue(0)]
        public int MaxLength { get; set; } = 0;

        /// <summary>
        /// 預設值。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("預設值。")]
        [DefaultValue("")]
        public string DefaultValue { get; set; } = string.Empty;

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
        /// 欄位關連的程式代碼。
        /// </summary>
        [XmlAttribute]
        [Category("Relation")]
        [Description("欄位關連的程式代碼。")]
        [DefaultValue("")]
        public string RelationProgId { get; set; } = string.Empty;

        /// <summary>
        /// 關聯來源欄位與本表欄位的對應集合。
        /// </summary>
        [Category("Relation")]
        [Description("關聯來源欄位與本表欄位的對應集合。")]
        [DefaultValue(null)]
        public FieldMappingCollection RelationFieldMappings
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(SerializeState, _relationFieldMappings)) { return null; }
                if (_relationFieldMappings == null) { _relationFieldMappings = new FieldMappingCollection(); }
                return _relationFieldMappings;
            }
        }

        /// <summary>
        /// 指定 UI 查詢/選取（Lookup）視窗的程式代碼（ProgID）。
        /// 當欄位需要透過彈出視窗選取資料時，設定此屬性以決定開啟哪個查詢/選取視窗。
        /// </summary>
        [XmlAttribute]
        [Category("Relation")]
        [Description("指定 UI 查詢/選取（Lookup）視窗的程式代碼。")]
        [DefaultValue("")]
        public string LookupProgId { get; set; } = string.Empty;

        /// <summary>
        /// 查詢/選取（Lookup）視窗回填欄位對應集合。
        /// 用於定義從查詢/選取視窗取回資料後，來源欄位與本表欄位的對應關係，將選取結果自動回填至本表指定欄位。
        /// </summary>
        [Category("Relation")]
        [Description("查詢/選取（Lookup）視窗回填欄位對應集合。")]
        [DefaultValue(null)]
        public FieldMappingCollection LookupFieldMappings
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(SerializeState, _lookupFieldMappings)) { return null; }
                if (_lookupFieldMappings == null) { _lookupFieldMappings = new FieldMappingCollection(); }
                return _lookupFieldMappings;
            }
        }

        /// <summary>
        /// 欄寬，設定值大於 0 才有效。
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("欄寬，設定值大於 0 才有效。")]
        [DefaultValue(0)]
        public int Width { get; set; } = 0;

        /// <summary>
        /// 所屬資料表。
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        [TreeNodeIgnore]
        public FormTable Table
        {
            get
            {
                if (Collection == null) { return null; }
                return (Collection as FormFieldCollection).Owner as FormTable;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_relationFieldMappings, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{FieldName} - {Caption}";
        }
    }
}
