using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Table index collection.
    /// </summary>
    [TreeNode("Indexes", true)]
    [Serializable]
    public class TableSchemaIndexCollection : KeyCollectionBase<TableSchemaIndex>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaIndexCollection"/>.
        /// </summary>
        /// <param name="tableSchema">The table schema that owns this collection.</param>
        public TableSchemaIndexCollection(TableSchema tableSchema) : base(tableSchema)
        { }

        /// <summary>
        /// Adds a primary key index.
        /// </summary>
        /// <param name="fields">Comma-separated field name string.</param>
        public TableSchemaIndex AddPrimaryKey(string fields)
        {
            var index = new TableSchemaIndex()
            {
                Name = "pk_{0}",
                Unique = true,
                PrimaryKey = true
            };

            string[] fieldNames = StrFunc.Split(fields, ",");
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
        public TableSchemaIndex Add(string name, string fields, bool unique)
        {
            TableSchemaIndex oIndex;
            string[] oFields;

            oIndex = new TableSchemaIndex();
            oIndex.Name = name;
            oIndex.Unique = unique;
            oFields = StrFunc.Split(fields, ",");
            foreach (string fieldName in oFields)
                oIndex.IndexFields!.Add(fieldName);
            Add(oIndex);
            return oIndex;
        }
    }
}
