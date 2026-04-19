using Bee.Definition.Database;
using Bee.Base.Data;
using Bee.Base.Collections;
using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// A collection of <see cref="DbParameterSpec"/> instances.
    /// </summary>
    public class DbParameterSpecCollection : KeyCollectionBase<DbParameterSpec>
    {
        /// <summary>
        /// Adds a parameter, inferring the DbType from the value.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        public DbParameterSpec Add(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name));

            var parameter = new DbParameterSpec(name, value);
            Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Adds a parameter based on a field definition.
        /// </summary>
        /// <param name="field">The field definition.</param>
        /// <param name="sourceVersion">The DataRow version to use when reading the value.</param>
        public DbParameterSpec Add(DbField field, DataRowVersion sourceVersion = DataRowVersion.Current)
        {
            var parameter = new DbParameterSpec()
            {
                Name = field.FieldName,
                DbType = DbTypeConverter.ToDbType(field.DbType),
                SourceColumn = field.FieldName,
                SourceVersion = sourceVersion,
                Value = field.AllowNull ? null : DataSetFunc.GetDefaultValue(field.DbType),
                Size = (field.DbType == FieldDbType.String) ? field.Length : 0,
            };
            Add(parameter);
            return parameter;
        }
    }
}
