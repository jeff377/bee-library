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
    public class TFormFieldCollection : TKeyCollectionBase<TFormField>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        public TFormFieldCollection(TFormTable formTable) : base(formTable)
        { }

        /// <summary>
        /// 加入欄位。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="dbType">欄位資料型別。</param>
        public TFormField Add(string fieldName, string caption, EFieldDbType dbType)
        {
            TFormField oField;

            oField = new TFormField(fieldName, caption, dbType);
            base.Add(oField);
            return oField;
        }

        /// <summary>
        /// 加入字串欄位。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">標題文字。</param>
        /// <param name="maxLength">字串最大長度。</param>
        public TFormField AddStringField(string fieldName, string caption, int maxLength)
        {
            TFormField oField;

            oField = new TFormField(fieldName, caption, EFieldDbType.String);
            oField.MaxLength = maxLength;
            base.Add(oField);
            return oField;
        }
    }
}
