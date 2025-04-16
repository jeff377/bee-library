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
            string sFilePath;

            sFilePath = DefinePathInfo.GetDbTableSettingsFilePath();
            ValidateFilePath(sFilePath);
            return SerializeFunc.XmlFileToObject<TDbSchemaSettings>(sFilePath);
        }

        /// <summary>
        /// 儲存資料表清單。
        /// </summary>
        /// <param name="settings">資料表清單。</param>
        public void SaveDbSchemaSettings(TDbSchemaSettings settings)
        {
            string sFilePath;

            sFilePath = DefinePathInfo.GetDbTableSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, sFilePath);
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public TDbTable GetDbTable(string dbName, string tableName)
        {
            TDbTable oValue;
            string sFilePath;

            sFilePath = DefinePathInfo.GetDbTableFilePath(dbName, tableName);
            ValidateFilePath(sFilePath);
            oValue = SerializeFunc.XmlFileToObject<TDbTable>(sFilePath);
            return oValue;
        }

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        public void SaveDbTable(string dbName, TDbTable dbTable)
        {
            string sFilePath;

            sFilePath = DefinePathInfo.GetDbTableFilePath(dbName, dbTable.TableName);
            SerializeFunc.ObjectToXmlFile(dbTable, sFilePath);
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        public TFormDefine GetFormDefine(string progID)
        {
            string sFilePath;

            sFilePath = DefinePathInfo.GetFormDefineFilePath(progID);
            ValidateFilePath(sFilePath);
            return SerializeFunc.XmlFileToObject<TFormDefine>(sFilePath);
        }

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public void SaveFormDefine(TFormDefine formDefine)
        {
            string sFilePath;

            sFilePath = DefinePathInfo.GetFormDefineFilePath(formDefine.ProgID);
            SerializeFunc.ObjectToXmlFile(formDefine, sFilePath);
        }

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutID">版面代碼。</param>
        public TFormLayout GetFormLayout(string layoutID)
        {
            string sFilePath;

            sFilePath = DefinePathInfo.GetFormLayoutFilePath(layoutID);
            ValidateFilePath(sFilePath);
            return SerializeFunc.XmlFileToObject<TFormLayout>(sFilePath);
        }

        /// <summary>
        /// 儲存表單版面配置。
        /// </summary>
        /// <param name="formLayout">表單版面配置。</param>
        public void SaveFormLayout(TFormLayout formLayout)
        {
            string sFilePath;

            sFilePath = DefinePathInfo.GetFormLayoutFilePath(formLayout.LayoutID);
            SerializeFunc.ObjectToXmlFile(formLayout, sFilePath);
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
