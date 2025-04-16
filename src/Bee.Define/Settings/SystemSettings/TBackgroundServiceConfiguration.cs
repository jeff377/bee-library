using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 背景服務程式參數及環境設置。
    /// </summary>
    [Serializable]
    [XmlType("BackgroundServiceConfiguration")]
    [Description("背景服務程式參數及環境設置。")]
    [TreeNode("背景服務")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class TBackgroundServiceConfiguration
    {
        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return "BackgroundServiceConfiguration";
        }
    }
}
