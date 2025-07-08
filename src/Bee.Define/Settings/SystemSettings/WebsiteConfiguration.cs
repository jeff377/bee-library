using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 網站參數及環境設置。
    /// </summary>
    [Serializable]
    [XmlType("WebsiteConfiguration")]
    [Description("網站參數及環境設置。")]
    [TreeNode("Website")]
    public class WebsiteConfiguration
    {
        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return "WebsiteConfiguration";
        }
    }
}
