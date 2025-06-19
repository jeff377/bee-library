using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 定義檔路徑資訊。
    /// </summary>
    public static class DefinePathInfo
    {
        /// <summary>
        /// 取得定義資料路徑或子路徑。
        /// </summary>
        /// <param name="subPath">子路徑。</param>
        private static string GetDefinePath(string subPath)
        {
            return FileFunc.PathCombine(BackendInfo.DefinePath, subPath);
        }

        /// <summary>
        /// 取得系統設定的檔案路徑。
        /// </summary>
        public static string GetSystemSettingsFilePath()
        {
            string sFileName;

            sFileName = "SystemSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// 取得資料庫設定的檔案路徑。
        /// </summary>
        public static string GetDatabaseSettingsFilePath()
        {
            string sFileName;

            sFileName = "DatabaseSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// 取得程式清單的檔案路徑。
        /// </summary>
        public static string GetProgramSettingsFilePath()
        {
            string sFileName;

            sFileName = "ProgramSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// 取得資料表清單的檔案路徑。
        /// </summary>
        public static string GetDbTableSettingsFilePath()
        {
            string sFileName;

            sFileName = "DbSchemaSettings.xml";
            return GetDefinePath(sFileName);
        }

        /// <summary>
        /// 取得資料表結構的檔案路徑。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public static string GetDbTableFilePath(string dbName, string tableName)
        {
            string sFilePath;

            sFilePath = $@"DbTable\{dbName}\{tableName}.DbTable.xml";
            return GetDefinePath(sFilePath);
        }

        /// <summary>
        /// 取得表單定義的檔案路徑。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public static string GetFormDefineFilePath(string progId)
        {
            string sFilePath;

            sFilePath = $@"FormDefine\{progId}.FormDefine.xml";
            return GetDefinePath(sFilePath);
        }

        /// <summary>
        /// 取得表單版面配置的檔案路徑。
        /// </summary>
        /// <param name="layoutID">版面代碼。</param>
        public static string GetFormLayoutFilePath(string layoutID)
        {
            string sFilePath;

            sFilePath = $@"FormLayout\{layoutID}.FormLayout.xml";
            return GetDefinePath(sFilePath);
        }
    }
}
