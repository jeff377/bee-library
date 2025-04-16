using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 參數項目。
    /// </summary>
    [Serializable]
    [XmlType("Parameter")]
    [DefaultProperty("Value")]
    public class TParameter : TKeyCollectionItem
    {
        private object _Value = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TParameter()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="value">參數值。</param>
        public TParameter(string name, object value)
        {
            this.Name = name;
            _Value = value;
        }

        #endregion

        /// <summary>
        /// 參數名稱。
        /// </summary>
        public string Name
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 參數值。
        /// </summary>        
        public object Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        /// <summary>
        /// 物件的描述文字。
        /// </summary>
        public override string ToString()
        {
            return StrFunc.Format("{0}={1}", this.Name, this.Value);
        }
    }
}
