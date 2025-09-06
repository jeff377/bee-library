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
        /// Logging level.
        /// Error: log only errors and exceptions.
        /// Warning: log errors, exceptions, and abnormal cases (slow queries, large updates).
        /// </summary>
        [Description("Logging level (Error: only errors, Warning: includes abnormal cases).")]
        public DbAccessLogLevel Level { get; set; } = DbAccessLogLevel.Warning;

        /// <summary>
        /// Threshold for the number of rows affected by SQL operations.
        /// Exceeding this value is considered a large update (default: 10000).
        /// </summary>
        [Description("Threshold for the number of rows affected by SQL operations. Exceeding this value is considered a large update (default: 10000).")]
        public int AffectedRowThreshold { get; set; } = 10000;

        /// <summary>
        /// Threshold for SQL command execution time (in seconds).
        /// Exceeding this value is considered a slow execution (applies to SELECT, INSERT, UPDATE, DELETE).
        /// </summary>
        [Description("Threshold for SQL command execution time (in seconds). Applies to SELECT, INSERT, UPDATE, DELETE.")]
        public int ExecutionTimeThreshold { get; set; } = 300;

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }

}
