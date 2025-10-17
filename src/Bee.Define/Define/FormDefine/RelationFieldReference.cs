using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 用於記錄關聯欄位的參照來源。
    /// </summary>
    public class RelationFieldReference : KeyCollectionItem
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public RelationFieldReference() { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">關聯欄位的名稱。</param>
        /// <param name="foreignKeyField">外鍵欄位。</param>
        /// <param name="sourceProgId">關聯來源的程式代碼。</param>
        /// <param name="sourceField">關聯來源的欄位名稱。</param>
        public RelationFieldReference(string fieldName, FormField foreignKeyField, string sourceProgId, string sourceField)
        {
            FieldName = fieldName;
            ForeignKeyField = foreignKeyField;
            SourceProgId = sourceProgId;
            SourceField = sourceField;
        }

        /// <summary>
        /// 關聯欄位的名稱。
        /// </summary>
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 外鍵欄位。
        /// </summary>
        public FormField ForeignKeyField { get; set; }

        /// <summary>
        /// 關聯來源的程式代碼。
        /// </summary>
        public string SourceProgId { get; set; }

        /// <summary>
        /// 關聯來源的欄位名稱。
        /// </summary>
        public string SourceField { get; set; }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{SourceProgId}.{SourceField} -> {FieldName}";
        }
    }
}
