namespace Bee.Define
{
    /// <summary>
    /// 定義資料存取介面。
    /// </summary>
    public interface IDefineAccess
    {
        /// <summary>
        /// 取得系統設定。
        /// </summary>
        TSystemSettings GetSystemSettings();

        /// <summary>
        /// 儲存系統設定。
        /// </summary>
        /// <param name="settings">系統設定。</param>
        void SaveSystemSettings(TSystemSettings settings);

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        TDatabaseSettings GetDatabaseSettings();

        /// <summary>
        /// 儲存資料庫設定。
        /// </summary>
        /// <param name="settings">資料庫設定。</param>
        void SaveDatabaseSettings(TDatabaseSettings settings);

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        TProgramSettings GetProgramSettings();

        /// <summary>
        /// 儲存程式清單。
        /// </summary>
        /// <param name="settings">程式清單。</param>
        void SaveProgramSettings(TProgramSettings settings);

        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        TDbSchemaSettings GetDbSchemaSettings();

        /// <summary>
        /// 儲存資料庫結構設定。
        /// </summary>
        /// <param name="settings">資料庫結構設定。</param>
        void SaveDbSchemaSettings(TDbSchemaSettings settings);

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        TDbTable GetDbTable(string dbName, string tableName);

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        void SaveDbTable(string dbName, TDbTable dbTable);

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        TFormDefine GetFormDefine(string progID);

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        void SaveFormDefine(TFormDefine formDefine);

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutID">版面代碼。</param>
        TFormLayout GetFormLayout(string layoutID);

        /// <summary>
        /// 儲存表單版面配置。
        /// </summary>
        /// <param name="formLayout">表單版面配置。</param>
        void SaveFormLayout(TFormLayout formLayout);
    }
}
