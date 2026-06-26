using System.Data;
using Bee.Base;
using Bee.Base.Collections;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// List item collection.
    /// </summary>
    public class ListItemCollection : KeyCollectionBase<ListItem>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ListItemCollection"/>.
        /// </summary>
        public ListItemCollection()
        { }

        #endregion

        /// <summary>
        /// Populates items from a data table.
        /// </summary>
        /// <param name="table">The data table.</param>
        /// <param name="valueField">The field name mapped to item value.</param>
        /// <param name="textField">The field name mapped to display text.</param>
        public void FromTable(DataTable table, string valueField, string textField)
        {
            foreach (DataRow row in table.Rows)
            {
                this.Add(ValueUtilities.CStr(row[valueField]), ValueUtilities.CStr(row[textField]));
            }
        }
    }

    /// <summary>
    /// Extension methods for <see cref="ListItemCollection"/>.
    /// </summary>
    public static class ListItemCollectionExtensions
    {
        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="value">The item value.</param>
        /// <param name="text">The display text.</param>
        public static ListItem Add(this ListItemCollection? collection, string value, string text)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var item = new ListItem(value, text);
            collection.Add(item);
            return item;
        }
    }
}
