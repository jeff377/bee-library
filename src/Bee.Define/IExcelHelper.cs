using System.Data;

namespace Bee.Define
{
    /// <summary>
    /// Interface for an Excel document operation helper.
    /// </summary>
    public interface IExcelHelper
    {
        /// <summary>
        /// Opens a file from a byte array.
        /// </summary>
        /// <param name="fileBytes">The file data.</param>
        void Open(byte[] fileBytes);

        /// <summary>
        /// Opens a file by file name.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        void Open(string fileName);

        /// <summary>
        /// Closes the file and releases resources.
        /// </summary>
        void Close();

        /// <summary>
        /// Saves the file as a byte array.
        /// </summary>
        /// <param name="sPassword">The open password.</param>
        byte[] SaveToBytes(string sPassword = "");

        /// <summary>
        /// Exports the data from the specified worksheet to a DataTable.
        /// </summary>
        /// <param name="sheetName">The worksheet name.</param>
        /// <param name="topRowisDisplayName">Indicates whether the first row contains display names, with field names in the second row.</param>
        DataTable ExportDataTable(string sheetName, bool topRowisDisplayName);

        /// <summary>
        /// Exports the data from the specified worksheet to a DataTable.
        /// </summary>
        /// <param name="sheetName">The worksheet name.</param>
        /// <param name="topRowIndex">The top starting row index, zero-based.</param>
        /// <param name="leftColumnIndex ">The left starting column index, zero-based.</param>
        DataTable ExportDataTable(string sheetName, int topRowIndex = 0, int leftColumnIndex = 0);

        /// <summary>
        /// Exports all worksheets to a DataSet.
        /// </summary>
        /// <param name="topRowIndex">The top starting row index, zero-based.</param>
        /// <param name="leftColumnIndex ">The left starting column index, zero-based.</param>
        DataSet ExportDataSet(int topRowIndex = 0, int leftColumnIndex = 0);

        /// <summary>
        /// Determines whether the specified worksheet exists.
        /// </summary>
        /// <param name="sheetName">The worksheet name.</param>
        bool HasWorksheet(string sheetName);

        /// <summary>
        /// Sets the active worksheet.
        /// </summary>
        /// <param name="index">The worksheet index.</param>
        void SetActiveWorksheet(int index);

        /// <summary>
        /// Inserts columns at the specified position.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <param name="count">The number of columns to insert.</param>
        void InsertColumn(int columnIndex, int count);

        /// <summary>
        /// Sets the value of the specified cell.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <param name="value">The value.</param>
        void SetCellValue(int row, int column, object value);

        /// <summary>
        /// Gets the value of the specified cell.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        object GetCellValue(int row, int column);
    }
}
