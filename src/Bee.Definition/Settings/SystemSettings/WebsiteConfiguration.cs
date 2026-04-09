using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Website parameters and environment settings.
    /// </summary>
    [Serializable]
    [XmlType("WebsiteConfiguration")]
    [Description("Website parameters and environment settings.")]
    [TreeNode("Website")]
    public class WebsiteConfiguration
    {
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
