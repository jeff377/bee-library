using Bee.Base;
using System;
using System.ComponentModel;

namespace Bee.Define
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
