using Bee.Define.Database;
using Bee.Define.Forms;
using Bee.Define.Layouts;
using Bee.Define.Settings;
using System.IO;
using Bee.Base;
using Bee.Base.Serialization;

namespace Bee.Define.Storage
{
    /// <summary>
    /// 以檔案為儲存媒介，實作定義資料的讀取與儲存。
    /// 提供資料庫結構、資料表結構、表單定義及表單版面配置等物件的檔案存取功能。
    /// 主要透過 XML 檔案序列化與反序列化方式，管理各類定義資料的持久化。
    /// </summary>
    public class FileDefineStorage : IDefineStorage
    {
        /// <summary>
        /// 取得資料表清單。
        /// </summary>
        public DbSchemaSettings GetDbSchemaSettings()
        {
            string filePath = DefinePathInfo.GetDbTableSettingsFilePath();
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<DbSchemaSettings>(filePath);
        }

        /// <summary>
        /// 儲存資料表清單。
        /// </summary>
        /// <param name="settings">資料表清單。</param>
        public void SaveDbSchemaSettings(DbSchemaSettings settings)
        {
            string filePath = DefinePathInfo.GetDbTableSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, filePath);
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public TableSchema GetTableSchema(string dbName, string tableName)
        {
            string filePath = DefinePathInfo.GetTableSchemaFilePath(dbName, tableName);
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<TableSchema>(filePath);
        }

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableSchema">資料表結構。</param>
        public void SaveTableSchema(string dbName, TableSchema tableSchema)
        {
            string filePath = DefinePathInfo.GetTableSchemaFilePath(dbName, tableSchema.TableName);
            SerializeFunc.ObjectToXmlFile(tableSchema, filePath);
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public FormSchema GetFormSchema(string progId)
        {
            string filePath = DefinePathInfo.GetFormSchemaFilePath(progId);
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<FormSchema>(filePath);
        }

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formSchema">表單定義。</param>
        public void SaveFormSchema(FormSchema formSchema)
        {
            string filePath = DefinePathInfo.GetFormSchemaFilePath(formSchema.ProgId);
            SerializeFunc.ObjectToXmlFile(formSchema, filePath);
        }

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutId">表單版面代碼。</param>
        public FormLayout GetFormLayout(string layoutId)
        {
            string filePath = DefinePathInfo.GetFormLayoutFilePath(layoutId);
            ValidateFilePath(filePath);
            return SerializeFunc.XmlFileToObject<FormLayout>(filePath);
        }

        /// <summary>
        /// 儲存表單版面配置。
        /// </summary>
        /// <param name="formLayout">表單版面配置。</param>
        public void SaveFormLayout(FormLayout formLayout)
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
