using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 透過 Cache 進行定義資料存取。
    /// </summary>
    public class TCacheDefineAccess : IDefineAccess
    {
        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        public object GetDefine(DefineType defineType, string[] keys = null)
        {
            switch (defineType)
            {
                case DefineType.SystemSettings:
                    return this.GetSystemSettings();
                case DefineType.DatabaseSettings:
                    return this.GetDatabaseSettings();
                case DefineType.ProgramSettings:
                    return  this.GetProgramSettings();
                case DefineType.DbSchemaSettings:
                    return this.GetDbSchemaSettings();
                case DefineType.DbTable:
                    ValidateKeys(defineType, keys, 2);
                    return  this.GetDbTable(keys[0], keys[1]);
                case DefineType.FormDefine:
                    ValidateKeys(defineType, keys, 1);
                    return this.GetFormDefine(keys[0]);
                case DefineType.FormLayout:
                    ValidateKeys(defineType, keys, 1);
                    return  this.GetFormLayout(keys[0]);
                default:
                    throw new NotSupportedException($"DefineType '{defineType}' is not supported.");
            }
        }

        /// <summary>
        /// 驗證鍵值的長度。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        /// <param name="expectedLength">正確鍵值的長度。</param>
        private void ValidateKeys(DefineType defineType, string[] keys, int expectedLength)
        {
            if (keys == null || keys.Length != expectedLength)
                throw new ArgumentException($"{defineType} keys verification error. Input: {string.Join(",", keys ?? new string[0])}");
        }

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        public void SaveDefine(DefineType defineType, object defineObject, string[] keys = null)
        {
            switch (defineType)
            {
                case DefineType.SystemSettings:
                    this.SaveSystemSettings(defineObject as SystemSettings);
                    break;
                case DefineType.DatabaseSettings:
                    this.SaveDatabaseSettings(defineObject as DatabaseSettings);
                    break;
                case DefineType.ProgramSettings:
                    this.SaveProgramSettings(defineObject as ProgramSettings);
                    break;
                case DefineType.DbSchemaSettings:
                    this.SaveDbSchemaSettings(defineObject as DbSchemaSettings);
                    break;
                case DefineType.DbTable:
                    if (keys == null || keys.Length != 1)
                        throw new ArgumentException($"{defineType} keys verification error");
                    this.SaveDbTable(keys[0], defineObject as DbTable);
                    break;
                case DefineType.FormLayout:
                    this.SaveFormLayout(defineObject as FormLayout);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 取得系統設定。
        /// </summary>
        public SystemSettings GetSystemSettings()
        {
            return CacheFunc.GetSystemSettings();
        }

        /// <summary>
        /// 儲存系統設定。
        /// </summary>
        /// <param name="settings">系統設定。</param>
        public void SaveSystemSettings(SystemSettings settings)
        {
            // 儲存系統設定
            string filePath = DefinePathInfo.GetSystemSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, filePath);
            // 移除快取
            var cache = new TSystemSettingsCache();
            cache.Remove();
        }

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        public DatabaseSettings GetDatabaseSettings()
        {
            return CacheFunc.GetDatabaseSettings();
        }

        /// <summary>
        /// 儲存資料庫設定。
        /// </summary>
        /// <param name="settings">資料庫設定。</param>
        public void SaveDatabaseSettings(DatabaseSettings settings)
        {
            TDatabaseSettingsCache oCache;
            string sFilePath;

            // 儲存資料庫設定後
            sFilePath = DefinePathInfo.GetDatabaseSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, sFilePath);
            // 移除快取
            oCache = new TDatabaseSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        public ProgramSettings GetProgramSettings()
        {
            return CacheFunc.GetProgramSettings();
        }

        /// <summary>
        /// 儲存程式清單。
        /// </summary>
        /// <param name="settings">程式清單。</param>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            TProgramSettingsCache oCache;
            string sFilePath;

            // 儲存程式清單後，移除快取
            sFilePath = DefinePathInfo.GetProgramSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, sFilePath);
            oCache = new TProgramSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        public DbSchemaSettings GetDbSchemaSettings()
        {
            return CacheFunc.GetDbSchemaSettings();
        }

        /// <summary>
        /// 儲存資料庫結構設定。
        /// </summary>
        /// <param name="settings">資料庫結構設定。</param>
        public void SaveDbSchemaSettings(DbSchemaSettings settings)
        {
            TDbSchemaSettingsCache oCache;

            // 儲存資料庫結構設定後，移除快取
            BackendInfo.DefineProvider.SaveDbSchemaSettings(settings);
            oCache = new TDbSchemaSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public DbTable GetDbTable(string dbName, string tableName)
        {
            return CacheFunc.GetDbTable(dbName, tableName);
        }

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        public void SaveDbTable(string dbName, DbTable dbTable)
        {
            TDbTableCache oCache;

            // 儲存資料表結構後，移除快取
            BackendInfo.DefineProvider.SaveDbTable(dbName, dbTable);
            oCache = new TDbTableCache();
            oCache.Remove(dbName, dbTable.TableName);
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public FormDefine GetFormDefine(string progId)
        {
            return CacheFunc.GetFormDefine(progId);
        }

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public void SaveFormDefine(FormDefine formDefine)
        {
            TFormDefineCache oCache;

            // 儲存資料表結構後，移除快取
            BackendInfo.DefineProvider.SaveFormDefine(formDefine);
            oCache = new TFormDefineCache();
            oCache.Remove(formDefine.ProgId);
        }

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutId">表單版面代碼。</param>
        public FormLayout GetFormLayout(string layoutId)
        {
            return CacheFunc.GetFormLayout(layoutId);
        }

        /// <summary>
        /// 儲存表單版面配置。
        /// </summary>
        /// <param name="formLayout">表單版面配置。</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            TFormLayoutCache oCache;

            // 儲存表單版面配置後，移除快取
            BackendInfo.DefineProvider.SaveFormLayout(formLayout);
            oCache = new TFormLayoutCache();
            oCache.Remove(formLayout.LayoutId);
        }
    }
}
