using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Define
{
    /// <summary>
    /// 參數項目。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [XmlType("Parameter")]
    [DefaultProperty("Value")]
    public class Parameter : KeyCollectionItem
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public Parameter()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="value">參數值。</param>
        public Parameter(string name, object value)
        {
            this.Name = name;
            Value = value;
        }

        #endregion

        /// <summary>
        /// 參數名稱。
        /// </summary>
        [Key(100)]
        public string Name
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 參數值。
        /// </summary>        
        [Key(101)]
        [MessagePackFormatter(typeof(TypelessFormatter))]
        public object Value { get; set; } = null;

        /// <summary>
        /// 物件的描述文字。
        /// </summary>
        public override string ToString()
        {
            return StrFunc.Format("{0}={1}", this.Name, this.Value);
        }
    }
}
