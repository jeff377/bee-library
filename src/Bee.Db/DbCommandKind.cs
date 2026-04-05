namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令的執行種類。
    /// </summary>
    public enum DbCommandKind
    {
        /// <summary>
        /// 執行資料庫命令，不回傳查詢結果，只傳回異動筆數。
        /// </summary>
        NonQuery,
        /// <summary>
        /// 執行資料庫命令，並回傳單一標量值（例如 COUNT(*)）。
        /// </summary>
        Scalar,
        /// <summary>
        /// 執行資料庫命令，並回傳完整的資料表結果集。
        /// </summary>
        DataTable
    }
}
