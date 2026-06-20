using System.Data;

namespace Bee.UI.Avalonia.DataObjects
{
    /// <summary>
    /// Event data for <see cref="FormDataObject.RowAdded"/> and
    /// <see cref="FormDataObject.RowDeleted"/>. Identifies which table changed and which row
    /// was added or deleted.
    /// </summary>
    public class RowChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RowChangedEventArgs"/>.
        /// </summary>
        /// <param name="tableName">The table whose row set changed.</param>
        /// <param name="row">The row that was added or deleted.</param>
        public RowChangedEventArgs(string tableName, DataRow row)
        {
            TableName = tableName;
            Row = row;
        }

        /// <summary>
        /// Gets the name of the table whose row set changed.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Gets the row that was added or deleted. A deleted row carries
        /// <see cref="DataRowState.Deleted"/>; read original values via
        /// <see cref="DataRowVersion.Original"/> if needed.
        /// </summary>
        public DataRow Row { get; }
    }
}
