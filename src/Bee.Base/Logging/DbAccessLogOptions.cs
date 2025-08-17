using System;
using System.ComponentModel;

namespace Bee.Base
{
    /// <summary>
    /// Logging options for the DbAccess module.
    /// </summary>
    [Serializable]
    [Description("Logging options for the DbAccess module.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DbAccessLogOptions
    {
        /// <summary>
        /// Logging level (Error: errors only, Warning: includes exceptions, All: all SQL).
        /// </summary>
        [Description("Logging level (Error: errors only, Warning: includes exceptions, All: all SQL).")]
        public DbAccessLogLevel Level { get; set; } = DbAccessLogLevel.Warning;

        /// <summary>
        /// Abnormal threshold for the number of rows affected by SQL operations. Exceeding this value is considered abnormal (default: 10000).
        /// </summary>
        [Description("Abnormal threshold for the number of rows affected by SQL operations. Exceeding this value is considered abnormal (default: 10000).")] 
        public int AffectedRowThreshold { get; set; } = 10000;

        /// <summary>
        /// Abnormal threshold for query execution time (in seconds). Exceeding this value is considered a slow query.
        /// </summary>
        [Description("Abnormal threshold for query execution time (in seconds). Exceeding this value is considered a slow query.")]
        public int SlowQueryThreshold { get; set; } = 300;

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
