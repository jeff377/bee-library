using System.Data;

namespace Bee.Define
{
    /// <summary>
    /// Excel 文件操作輔助類別介面。
    /// </summary>
    public interface IExcelHelper
    {
        /// <summary>
        /// 開啟檔案。
        /// </summary>
        /// <param name="fileBytes">檔案資料。</param>
        void Open(byte[] fileBytes);

        /// <summary>
        /// 開啟檔案。
        /// </summary>
        /// <param name="fileName">檔案名稱。</param>
        void Open(string fileName);

        /// <summary>
        /// 關閉檔案並釋放資源。
        /// </summary>
        void Close();

        /// <summary>
        /// 儲存為二進位資料。
        /// </summary>
        /// <param name="sPassword">開啟密碼</param>        
        byte[] SaveToBytes(string sPassword = "");

        /// <summary>
        /// 將指定工作表的資料匯出至 DataTable。
        /// </summary>
        /// <param name="sheetName">工作表名稱。</param>
        /// <param name="topRowisDisplayName">第一列為顯示名稱，欄位名稱置於第二列。</param>
        DataTable ExportDataTable(string sheetName, bool topRowisDisplayName);

        /// <summary>
        /// 將指定工作表的資料匯出至 DataTable。
        /// </summary>
        /// <param name="sheetName">工作表名稱。</param>
        /// <param name="topRowIndex">上方起始列索引，起始為 0。</param>
        /// <param name="leftColumnIndex ">左邊起始欄索引，起始為 0。</param>
        DataTable ExportDataTable(string sheetName, int topRowIndex = 0, int leftColumnIndex = 0);

        /// <summary>
        /// 將所有工作表匯出至 DataSet。
        /// </summary>
        /// <param name="topRowIndex">上方起始列索引，起始為 0。</param>
        /// <param name="leftColumnIndex ">左邊起始欄索引，起始為 0。</param>
        DataSet ExportDataSet(int topRowIndex = 0, int leftColumnIndex = 0);

        /// <summary>
        /// 判斷是否有指定的工作表。
        /// </summary>
        /// <param name="sheetName">工作表名稱。</param>
        bool HasWorksheet(string sheetName);

        /// <summary>
        /// 設定作用工作表。
        /// </summary>
        /// <param name="index">工作表索引。</param>
        void SetActiveWorksheet(int index);

        /// <summary>
        /// 插入欄位。
        /// </summary>
        /// <param name="columnIndex">欄索引。</param>
        /// <param name="count">插入欄位數。</param>
        void InsertColumn(int columnIndex, int count);

        /// <summary>
        /// 設定指定儲存格的值。
        /// </summary>
        /// <param name="row">列索引。</param>
        /// <param name="column">欄索引。</param>
        /// <param name="value">值。</param>
        void SetCellValue(int row, int column, object value);

        /// <summary>
        /// 取得指定儲存格的值。
        /// </summary>
        /// <param name="row">列索引。</param>
        /// <param name="column">欄索引。</param>
        object GetCellValue(int row, int column);
    }
}
