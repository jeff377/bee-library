using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 欄位結構。
    /// </summary>
    [Serializable]
    [XmlType("DbField")]
    [Description("欄位結構。")]
    [TreeNode]
    public class TDbField : TKeyCollectionItem, IDefineField
    {
        private string _Caption = string.Empty;
        private EFieldDbType _DbType = EFieldDbType.String;
        private int _Length = 0;
        private bool _AllowNull = false;
        private string _DefaultValue = string.Empty;
        private EDbUpgradeAction _UpgradeAction = EDbUpgradeAction.None;

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TDbField()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="dbType">欄位資料型別。</param>
        public TDbField(string fieldName, string caption, EFieldDbType dbType)
        {
            this.FieldName = fieldName;
            _Caption = caption;
            _DbType = dbType;
        }

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [XmlAttribute]
        [Category(Category.Data)]
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
        [Category(Category.Data)]
        [NotifyParentProperty(true)]
        [Description("標題文字。")]
        public string Caption
        {
            get { return _Caption; }
            set { _Caption = value; }
        }

        /// <summary>
        /// 資料型別。
        /// </summary>
        [XmlAttribute]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        [Category(Category.Data)]
        [Description("資料型別。")]
        public EFieldDbType DbType
        {
            get { return _DbType; }
            set { _DbType = value; }
        }

        /// <summary>
        /// 字串型別的欄位長度。
        /// </summary>
        [XmlAttribute]
        [Category(Category.Data)]
        [Description("字串型別的欄位長度。")]
        [DefaultValue(0)]
        public int Length
        {
            get { return _Length; }
            set { _Length = value; }
        }

        /// <summary>
        /// 是否允許 Null 值。
        /// </summary>
        [XmlAttribute]
        [Category(Category.Data)]
        [Description("是否允許 Null 值。")]
        [DefaultValue(false)]
        public bool AllowNull
        {
            get { return _AllowNull; }
            set { _AllowNull = value; }
        }

        /// <summary>
        /// 預設值。
        /// </summary>
        [XmlAttribute]
        [Category(Category.Data)]
        [Description("預設值。")]
        [DefaultValue("")]
        public string DefaultValue
        {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        /// <summary>
        /// 欄位結構升級動作。
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(EDbUpgradeAction.None)]
        public EDbUpgradeAction UpgradeAction
        {
            get { return _UpgradeAction; }
            set { _UpgradeAction = value; }
        }

        /// <summary>
        /// 建立複本。
        /// </summary>
        public TDbField Clone()
        {
            TDbField oField;

            oField = new TDbField();
            oField.FieldName = this.FieldName;
            oField.Caption = this.Caption;
            oField.DbType = this.DbType;
            oField.Length = this.Length;
            oField.AllowNull = this.AllowNull;
            oField.DefaultValue = this.DefaultValue;
            return oField;
        }

        /// <summary>
        /// 比較結構是否相同。
        /// </summary>
        /// <param name="source">來源物件。</param>
        public bool Compare(TDbField source)
        {
            // 比對資料型別
            if (this.DbType != source.DbType) { return false; }
            // 比對是否允許 Null
            if (this.AllowNull != source.AllowNull) { return false; }
            // 比對文字型別的欄位長度
            if ((this.DbType == EFieldDbType.String) && (this.Length != source.Length))
                return false;
            // 比較預設值
            if (!StrFunc.IsEquals(this.DefaultValue, source.DefaultValue)) { return false; }

            return true;
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
