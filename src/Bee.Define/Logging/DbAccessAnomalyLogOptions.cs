using Bee.Base;
using System;
using System.ComponentModel;

namespace Bee.Define
{
    /// <summary>
    /// Options for logging abnormal SQL executions in the DbAccess module.
    /// Includes thresholds for affected rows, result rows, and execution time.
    /// </summary>
    [Serializable]
    [Description("Options for logging abnormal SQL executions in the DbAccess module.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DbAccessAnomalyLogOptions
    {
        /// <summary>
        /// Logging level for abnormal SQL executions.
        /// None: disable abnormal logging.
        /// Error: log only errors and exceptions.
        /// Warning: log errors, exceptions, and abnormal cases (slow queries, large updates, large result sets).
        /// </summary>
        [Description("Logging level (Error: only errors, Warning: includes abnormal cases).")]
        public DbAccessAnomalyLogLevel Level { get; set; } = DbAccessAnomalyLogLevel.Warning;

        /// <summary>
        /// Threshold for the number of rows affected by SQL operations.
        /// Exceeding this threshold will be logged as abnormal (default: 10000).
        /// A value less than or equal to 0 disables this check.
        /// </summary>
        [Description("Threshold for affected rows (default: 10000). <=0 disables this check.")]
        public int AffectedRowThreshold { get; set; } = 10000;

        /// <summary>
        /// Threshold for the number of rows returned by SELECT queries.
        /// Exceeding this value is considered an abnormal case (default: 10000).
        /// A value less than or equal to 0 disables logging for result rows.
        /// </summary>
        [Description("Threshold for result rows (default: 10000). <=0 disables logging for result rows.")]
        public int ResultRowThreshold { get; set; } = 10000;

        /// <summary>
        /// Threshold for SQL command execution time (in seconds).
        /// Exceeding this value is considered a slow execution (default: 300).
        /// Applies to SELECT, INSERT, UPDATE, DELETE.
        /// A value less than or equal to 0 disables logging for execution time.
        /// </summary>
        [Description("Threshold for execution time in seconds (default: 300). <=0 disables logging for execution time.")]
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
