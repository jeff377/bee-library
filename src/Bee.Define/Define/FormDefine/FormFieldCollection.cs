using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 表單欄位集合。
    /// </summary>
    [Serializable]
    [Description("表單欄位集合。")]
    [TreeNode("欄位", true)]
    public class FormFieldCollection : KeyCollectionBase<FormField>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        public FormFieldCollection(FormTable formTable) : base(formTable)
        { }

        /// <summary>
        /// 加入欄位。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="dbType">欄位資料型別。</param>
        public FormField Add(string fieldName, string caption, FieldDbType dbType)
        {
            FormField oField;

            oField = new FormField(fieldName, caption, dbType);
            base.Add(oField);
            return oField;
        }

        /// <summary>
        /// 加入字串欄位。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="maxLength">字串最大長度。</param>
        public FormField AddStringField(string fieldName, string caption, int maxLength)
        {
            FormField oField;

            oField = new FormField(fieldName, caption, FieldDbType.String);
            oField.MaxLength = maxLength;
            base.Add(oField);
            return oField;
        }
    }
}
