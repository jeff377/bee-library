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
    }

    /// <summary>
    /// Convenience extension methods for <see cref="DbParameterSpecCollection"/>.
    /// </summary>
    /// <remarks>
    /// These are extension methods rather than members because the collection may only expose a single
    /// public <c>Add</c>. XmlSerializer's reflection-only deserialization path resolves the add method
    /// with one <c>Type.GetMethod("Add")</c> call and throws on multiple public overloads, which affects
    /// AOT targets such as iOS. The rule is enforced at build time by BEE4005.
    /// </remarks>
    public static class DbParameterSpecCollectionExtensions
    {
        /// <summary>
        /// Adds a parameter, inferring the DbType from the value.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        public static DbParameterSpec Add(this DbParameterSpecCollection? collection, string name, object value)
        {
            ArgumentNullException.ThrowIfNull(collection);
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name));

            var parameter = new DbParameterSpec(name, value);
            collection.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Adds a parameter based on a field definition, reading the current DataRow version.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="field">The field definition.</param>
        public static DbParameterSpec Add(this DbParameterSpecCollection? collection, DbField field)
        {
            return collection.Add(field, DataRowVersion.Current);
        }

        /// <summary>
        /// Adds a parameter based on a field definition.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="field">The field definition.</param>
        /// <param name="sourceVersion">The DataRow version to use when reading the value.</param>
        public static DbParameterSpec Add(this DbParameterSpecCollection? collection, DbField field, DataRowVersion sourceVersion)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(field);

            var parameter = new DbParameterSpec()
            {
                Name = field.FieldName,
                DbType = DbTypeConverter.ToDbType(field.DbType),
                SourceColumn = field.FieldName,
                SourceVersion = sourceVersion,
                Value = field.AllowNull ? null : field.DbType.GetDefaultValue(),
                Size = (field.DbType == FieldDbType.String) ? field.Length : 0,
            };
            collection.Add(parameter);
            return parameter;
        }
    }
}
