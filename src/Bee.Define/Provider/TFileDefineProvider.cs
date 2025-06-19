using System.IO;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 檔案定義資料提供者。
    /// </summary>
    public class TFileDefineProvider : IDefineProvider
    {
        /// <summary>
        /// 取得資料表清單。
        /// </summary>
        public TDbSchemaSettings GetDbSchemaSettings()
        {
            string filePath = DefinePathInfo.GetDbTableSettingsFilePath();
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<TDbSchemaSettings>(filePath);
        }

        /// <summary>
        /// 儲存資料表清單。
        /// </summary>
        /// <param name="settings">資料表清單。</param>
        public void SaveDbSchemaSettings(TDbSchemaSettings settings)
        {
            string filePath = DefinePathInfo.GetDbTableSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, filePath);
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public TDbTable GetDbTable(string dbName, string tableName)
        {
            string filePath = DefinePathInfo.GetDbTableFilePath(dbName, tableName);
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<TDbTable>(filePath);
        }

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        public void SaveDbTable(string dbName, TDbTable dbTable)
        {
            string filePath = DefinePathInfo.GetDbTableFilePath(dbName, dbTable.TableName);
            SerializeFunc.ObjectToXmlFile(dbTable, filePath);
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public TFormDefine GetFormDefine(string progId)
        {
            string filePath = DefinePathInfo.GetFormDefineFilePath(progId);
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<TFormDefine>(filePath);
        }

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public void SaveFormDefine(TFormDefine formDefine)
        {
            string filePath = DefinePathInfo.GetFormDefineFilePath(formDefine.ProgId);
            SerializeFunc.ObjectToXmlFile(formDefine, filePath);
        }

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutId">表單版面代碼。</param>
        public TFormLayout GetFormLayout(string layoutId)
        {
            string filePath = DefinePathInfo.GetFormLayoutFilePath(layoutId);
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<TFormLayout>(filePath);
        }

        /// <summary>
        /// 儲存表單版面配置。
        /// </summary>
        /// <param name="formLayout">表單版面配置。</param>
        public void SaveFormLayout(TFormLayout formLayout)
        {
            string filePath = DefinePathInfo.GetFormLayoutFilePath(formLayout.LayoutId);
            SerializeFunc.ObjectToXmlFile(formLayout, filePath);
        }

        /// <summary>
        /// 驗證檔案是否存在。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        private void ValidateFilePath(string filePath)
        {
            if (!FileFunc.FileExists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");
        }
    }
}
