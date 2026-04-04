using Bee.Base;
using Bee.Base.Attributes;
using System;
using System.ComponentModel;

namespace Bee.Define.Logging
{
    /// <summary>
    /// Logging options for controlling whether each module logs information.
    /// </summary>
    [Serializable]
    [Description("Logging options for controlling whether each module logs information.")]
    [TreeNode("Logging")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class LogOptions
    {
        /// <summary>
        /// Logging options for the DbAccess module.
        /// </summary>
        [Description("Logging options for the DbAccess module.")]
        public DbAccessAnomalyLogOptions DbAccess { get; set; } = new DbAccessAnomalyLogOptions();

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
