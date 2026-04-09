using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Frontend parameters and environment settings.
    /// </summary>
    [Serializable]
    [XmlType("FrontendConfiguration")]
    [Description("Frontend parameters and environment settings.")]
    [TreeNode("Frontend")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class FrontendConfiguration
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
