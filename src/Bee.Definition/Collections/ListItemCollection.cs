using System;
using System.Data;
using Bee.Core;
using Bee.Core.Collections;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// List item collection.
    /// </summary>
    [Serializable]
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
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="value">The item value.</param>
        /// <param name="text">The display text.</param>
        public ListItem Add(string value, string text)
        {
            var item = new ListItem(value, text);
            Add(item);
            return item;
        }

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
                Add(BaseFunc.CStr(row[valueField]), BaseFunc.CStr(row[textField]));
            }
        }
    }
}
