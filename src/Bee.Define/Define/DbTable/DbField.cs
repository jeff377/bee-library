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
    public class DbField : KeyCollectionItem, IDefineField
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public DbField()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="dbType">欄位資料型別。</param>
        public DbField(string fieldName, string caption, FieldDbType dbType)
        {
            FieldName = fieldName;
            Caption = caption;
            DbType = dbType;
        }

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
        /// 字串型別的欄位長度，適用於 <see cref="FieldDbType.String"/>。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("字串型別的欄位長度，適用於 String。")]
        [DefaultValue(0)]
        public int Length { get; set; } = 0;

        /// <summary>
        /// 數值型別的精度（總位數），適用於 Decimal。
        /// </summary>
        /// <remarks>
        /// 適用於 Decimal 型別，表示數值的總位數，包含整數和小數部分。
        /// 例如：Precision=19, Scale=4 表示 DECIMAL(19,4)，範圍為 -999999999999999.9999 到 999999999999999.9999。
        /// </remarks>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("數值型別的精度（總位數），適用於 Decimal  型別。")]
        [DefaultValue(18)]
        public int Precision { get; set; } = 18;

        /// <summary>
        /// 數值型別的小數位數，適用於 Decimal。
        /// </summary>
        /// <remarks>
        /// 適用於 Decimal 型別，表示小數點後的位數。
        /// 例如：Precision=19, Scale=4 表示最多 4 位小數。
        /// </remarks>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("數值型別的小數位數，適用於 Decimal。")]
        [DefaultValue(0)]
        public int Scale { get; set; } = 0;


        /// <summary>
        /// 是否允許 Null 值。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("是否允許 Null 值。")]
        [DefaultValue(false)]
        public bool AllowNull { get; set; } = false;

        /// <summary>
        /// 預設值。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("預設值。")]
        [DefaultValue("")]
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// 欄位結構升級動作，執行階段比對資料欄位結構使用，此屬性不做序列化。
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(DbUpgradeAction.None)]
        public DbUpgradeAction UpgradeAction { get; set; } = DbUpgradeAction.None;

        /// <summary>
        /// 建立複本。
        /// </summary>
        public DbField Clone()
        {
            return new DbField
            {
                FieldName = FieldName,
                Caption = Caption,
                DbType = DbType,
                Length = Length,
                Precision = Precision,
                Scale = Scale,
                AllowNull = AllowNull,
                DefaultValue = DefaultValue
            };
        }

        /// <summary>
        /// 比較結構是否相同。
        /// </summary>
        /// <param name="source">來源物件。</param>
        public bool Compare(DbField source)
        {
            // 比對資料型別
            if (DbType != source.DbType) { return false; }
            // 比對是否允許 Null
            if (AllowNull != source.AllowNull) { return false; }
            // 比對 String 型別的欄位長度
            if ((DbType == FieldDbType.String) && (Length != source.Length))
                return false;
            // 比對 Decimal 型別的精度和小數位數
            if ((DbType == FieldDbType.Decimal) && (Precision != source.Precision || Scale != source.Scale))
                return false;
            // 比較預設值
            if (!StrFunc.IsEquals(DefaultValue, source.DefaultValue)) { return false; }

            return true;
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
