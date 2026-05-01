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
        /// <param name="dbTable">The table schema that owns this collection.</param>
        public DbFieldCollection(TableSchema dbTable)
          : base(dbTable)
        { }

        /// <summary>
        /// Adds a new field to the collection.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="dbType">The field database type.</param>
        /// <param name="length">The field length for string-type fields.</param>
        public DbField Add(string fieldName, string caption, FieldDbType dbType, int length = 0)
        {
            var dbField = new DbField(fieldName, caption, dbType)
            {
                Length = length
            };
            Add(dbField);
            return dbField;
        }
    }
}
