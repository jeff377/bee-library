using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

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
        [Category("Link")]
        [Description("欄位關連的程式代碼。")]
        [DefaultValue("")]
        public string RelationProgId { get; set; } = string.Empty;

        /// <summary>
        /// 關聯來源欄位與本表欄位的對應集合。
        /// 本表欄位應該
        /// </summary>
        [Category("Link")]
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
                if (this.Collection == null) { return null; }
                return (this.Collection as FormFieldCollection).Owner as FormTable;
            }
        }

        /// <summary>
        /// 加入關連取回設定。
        /// </summary>
        /// <param name="linkProgId">欄位關連的程式代碼。</param>
        /// <param name="sourceFields">來源欄位集合字串，以逗點分隔多個欄位。</param>
        /// <param name="destinationFields">目的欄位集合字串，以逗點分隔多個欄位。</param>
        public void AddLinkReturn(string linkProgId, string sourceFields, string destinationFields)
        {
            string[] oSourceFields, oDestinationFields;

            oSourceFields = StrFunc.Split(sourceFields, ",");
            oDestinationFields = StrFunc.Split(destinationFields, ",");
            if (oSourceFields.Length != oDestinationFields.Length)
                throw new InvalidOperationException("Source and destination fields must have the same number.");

            this.RelationProgId = linkProgId;
            this.RelationFieldMappings.Clear();
            for (int N1 = 0; N1 < oSourceFields.Length; N1++)
                this.RelationFieldMappings.Add(oSourceFields[N1], oDestinationFields[N1]);
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
            return $"{this.FieldName} - {this.Caption}";
        }
    }
}
