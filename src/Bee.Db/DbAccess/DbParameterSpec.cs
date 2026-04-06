using Bee.Base;
using Bee.Base.Collections;
using System;
using System.Data;
using System.Data.Common;

using Bee.Db;

namespace Bee.Db.DbAccess
{
    /// <summary>
    /// Describes a database command parameter as an intermediary for <see cref="DbParameter"/>.
    /// </summary>
    public class DbParameterSpec : KeyCollectionItem
    {
        /// <summary>
        /// Initializes a new empty instance of <see cref="DbParameterSpec"/>.
        /// </summary>
        public DbParameterSpec()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="DbParameterSpec"/> and infers the DbType from the value.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        public DbParameterSpec(string name, object value)
        {
            Name= name;
            Value= value;
            DbType = DbFunc.InferDbType(value);
        }

        /// <summary>
        /// Gets or sets the parameter name. Do not include the prefix character (e.g., @ for SQL Server).
        /// </summary>
        public string Name
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the parameter value; may be set to <c>null</c>.
        /// </summary>
        public object Value { get; set; } = null;

        /// <summary>
        /// Gets or sets the parameter data type. If <c>null</c>, the database provider infers the type automatically.
        /// </summary>
        public DbType? DbType { get; set; }

        /// <summary>
        /// Gets or sets the parameter size (applicable for string or binary data). Optional.
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// Gets or sets whether the parameter value may be <see cref="DBNull"/>.
        /// </summary>
        public bool IsNullable { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the source column in a <see cref="DataRow"/>, used for data binding and update operations.
        /// </summary>
        public string SourceColumn { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source row version that determines which value to use in update commands.
        /// For example: <see cref="DataRowVersion.Current"/>, <see cref="DataRowVersion.Original"/>, or <see cref="DataRowVersion.Proposed"/>.
        /// </summary>
        public DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }
}
