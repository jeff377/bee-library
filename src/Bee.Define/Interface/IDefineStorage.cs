namespace Bee.Define
{
    /// <summary>
    /// 定義資料儲存區介面。
    /// </summary>
    public interface IDefineStorage
    {
        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        DbSchemaSettings GetDbSchemaSettings();

        /// <summary>
        /// 儲存資料庫結構設定。
        /// </summary>
        /// <param name="settings">資料庫結構設定。</param>
        void SaveDbSchemaSettings(DbSchemaSettings settings);

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        DbTable GetDbTable(string dbName, string tableName);

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        void SaveDbTable(string dbName, DbTable dbTable);

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        FormDefine GetFormDefine(string progId);

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        void SaveFormDefine(FormDefine formDefine);

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutId">表單版面代碼。</param>
        FormLayout GetFormLayout(string layoutId);

        /// <summary>
        /// 儲存表單版面配置。
        /// </summary>
        /// <param name="formLayout">表單版面配置。</param>
        void SaveFormLayout(FormLayout formLayout);
    }
}
