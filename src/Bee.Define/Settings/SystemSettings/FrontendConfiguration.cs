using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 前端參數及環境設置。
    /// </summary>
    [Serializable]
    [XmlType("FrontendConfiguration")]
    [Description("前端參數及環境設置。")]
    [TreeNode("Frontend")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class FrontendConfiguration
    {
        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return "FrontendConfiguration";
        }
    }
}
