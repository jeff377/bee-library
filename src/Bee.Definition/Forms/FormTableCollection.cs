using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A collection of form tables.
    /// </summary>
    [Serializable]
    [Description("Form table collection.")]
    [TreeNode("Tables", false)]
    public class FormTableCollection : KeyCollectionBase<FormTable>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FormTableCollection"/>.
        /// </summary>
        /// <param name="formDefine">The owning form schema definition.</param>
        public FormTableCollection(FormSchema formDefine) : base(formDefine)
        { }


        /// <summary>
        /// Adds a table to the collection.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="displayName">The display name.</param>
        public FormTable Add(string tableName, string displayName)
        {
            var table = new FormTable(tableName, displayName);
            base.Add(table);
            return table;
        }
    }
}
