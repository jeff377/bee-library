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
    [TreeNode("BackgroundService")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackgroundServiceConfiguration
    {
        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
