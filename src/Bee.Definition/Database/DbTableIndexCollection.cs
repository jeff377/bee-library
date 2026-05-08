using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Table index collection.
    /// </summary>
    [TreeNode("Indexes", true)]
    public class DbTableIndexCollection : KeyCollectionBase<DbTableIndex>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DbTableIndexCollection"/>.
        /// </summary>
        /// <param name="tableSchema">The table schema that owns this collection.</param>
        public DbTableIndexCollection(TableSchema tableSchema) : base(tableSchema)
        { }

        /// <summary>
        /// Adds a primary key index.
        /// </summary>
        /// <param name="fields">Comma-separated field name string.</param>
        public DbTableIndex AddPrimaryKey(string fields)
        {
            var index = new DbTableIndex()
            {
                Name = "pk_{0}",
                Unique = true,
                PrimaryKey = true
            };

            string[] fieldNames = StringUtilities.Split(fields, ",");
            foreach (string fieldName in fieldNames)
                index.IndexFields!.Add(fieldName);
            Add(index);
            return index;
        }

        /// <summary>
        /// Adds an index.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="fields">Comma-separated field name string.</param>
        /// <param name="unique">Indicates whether the index is unique.</param>
        public DbTableIndex Add(string name, string fields, bool unique)
        {
            DbTableIndex oIndex;
            string[] oFields;

            oIndex = new DbTableIndex();
            oIndex.Name = name;
            oIndex.Unique = unique;
            oFields = StringUtilities.Split(fields, ",");
            foreach (string fieldName in oFields)
                oIndex.IndexFields!.Add(fieldName);
            Add(oIndex);
            return oIndex;
        }
    }
}
