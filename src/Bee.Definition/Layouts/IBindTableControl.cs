using System.Data;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Interface for a control that binds to a data table.
    /// </summary>
    public interface IBindTableControl
    {
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        string TableName { get; set; }

        /// <summary>
        /// Gets or sets the bound data table.
        /// </summary>
        DataTable? DataTable { get; set; }

        /// <summary>
        /// Ends the current edit operation.
        /// </summary>
        void EndEdit();
    }
}
