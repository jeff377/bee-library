using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 通用參數及環境設置。
    /// </summary>
    [Serializable]
    [XmlType("CommonConfiguration")]
    [Description("通用參數及環境設置。")]
    [TreeNode("通用")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class TCommonConfiguration
    {
        private string _Version = string.Empty;

        /// <summary>
        /// 系統版號。
        /// </summary>
        [Description("系統版號。")]
        public string Version
        {
            get { return _Version; }
            set { _Version = value; }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return "CommonConfiguration";
        }
    }
}
