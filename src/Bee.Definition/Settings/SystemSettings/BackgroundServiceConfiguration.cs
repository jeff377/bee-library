using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Background service parameters and environment settings.
    /// </summary>
    [Serializable]
    [XmlType("BackgroundServiceConfiguration")]
    [Description("Background service parameters and environment settings.")]
    [TreeNode("BackgroundService")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackgroundServiceConfiguration
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
