using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A collection of form tables.
    /// </summary>
    [Description("Form table collection.")]
    [TreeNode("Tables", false)]
    public class FormTableCollection : KeyCollectionBase<FormTable>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FormTableCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public FormTableCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="FormTableCollection"/>.
        /// </summary>
        /// <param name="formDefine">The owning form schema definition.</param>
        public FormTableCollection(FormSchema formDefine) : base(formDefine)
        { }


    }

    /// <summary>
    /// Convenience extension methods for <see cref="FormTableCollection"/>.
    /// </summary>
    public static class FormTableCollectionExtensions
    {
        /// <summary>
        /// Adds a table to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="displayName">The display name.</param>
        public static FormTable Add(this FormTableCollection? collection, string tableName, string displayName)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var table = new FormTable(tableName, displayName);
            collection.Add(table);
            return table;
        }
    }
}
