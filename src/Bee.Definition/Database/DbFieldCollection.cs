using Bee.Base.Attributes;
using Bee.Base.Data;
using Bee.Base.Collections;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Database field schema collection.
    /// </summary>
    [TreeNode("Fields", true)]
    public class DbFieldCollection : KeyCollectionBase<DbField>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DbFieldCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public DbFieldCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="DbFieldCollection"/>.
        /// </summary>
        /// <param name="dbTable">The table schema that owns this collection.</param>
        public DbFieldCollection(TableSchema dbTable)
          : base(dbTable)
        { }
    }

    /// <summary>
    /// Extension methods for <see cref="DbFieldCollection"/>.
    /// </summary>
    public static class DbFieldCollectionExtensions
    {
        /// <summary>
        /// Adds a new field to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="dbType">The field database type.</param>
        /// <param name="length">The field length for string-type fields.</param>
        public static DbField Add(this DbFieldCollection? collection, string fieldName, string caption, FieldDbType dbType, int length = 0)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var dbField = new DbField(fieldName, caption, dbType)
            {
                Length = length
            };
            collection.Add(dbField);
            return dbField;
        }
    }
}
