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
        public object GetDefine(EDefineType defineType, string[] keys = null)
        {
            object oValue;

            oValue = null;
            switch (defineType)
            {
                case EDefineType.SystemSettings:
                    oValue = this.GetSystemSettings();
                    break;
                case EDefineType.DatabaseSettings:
                    oValue = this.GetDatabaseSettings();
                    break;
                case EDefineType.ProgramSettings:
                    oValue = this.GetProgramSettings();
                    break;
                case EDefineType.DbSchemaSettings:
                    oValue = this.GetDbSchemaSettings();
                    break;
                case EDefineType.DbTable:
                    if (keys == null || keys.Length != 2)
                        throw new TException($"{defineType} Keys verification error");
                    oValue = this.GetDbTable(keys[0], keys[1]);
                    break;
                case EDefineType.FormDefine:
                    if (keys == null || keys.Length != 1)
                        throw new TException($"{defineType} Keys verification error");
                    oValue = this.GetFormDefine(keys[0]);
                    break;
                case EDefineType.FormLayout:
                    if (keys == null || keys.Length != 1)
                        throw new TException($"{defineType} Keys verification error");
                    oValue = this.GetFormLayout(keys[0]);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return oValue;
        }

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        public void SaveDefine(EDefineType defineType, object defineObject, string[] keys = null)
        {
            switch (defineType)
            {
                case EDefineType.SystemSettings:
                    this.SaveSystemSettings(defineObject as TSystemSettings);
                    break;
                case EDefineType.DatabaseSettings:
                    this.SaveDatabaseSettings(defineObject as TDatabaseSettings);
                    break;
                case EDefineType.ProgramSettings:
                    this.SaveProgramSettings(defineObject as TProgramSettings);
                    break;
                case EDefineType.DbSchemaSettings:
                    this.SaveDbSchemaSettings(defineObject as TDbSchemaSettings);
                    break;
                case EDefineType.DbTable:
                    if (keys == null || keys.Length != 1)
                        throw new TException($"{defineType} Keys verification error");
                    this.SaveDbTable(keys[0], defineObject as TDbTable);
                    break;
                case EDefineType.FormLayout:
                    this.SaveFormLayout(defineObject as TFormLayout);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 取得系統設定。
        /// </summary>
        public TSystemSettings GetSystemSettings()
        {
            return CacheFunc.GetSystemSettings();
        }

        /// <summary>
        /// 儲存系統設定。
        /// </summary>
        /// <param name="settings">系統設定。</param>
        public void SaveSystemSettings(TSystemSettings settings)
        {
            TSystemSettingsCache oCache;
            string sFilePath;

            // 儲存系統設定後，移除快取
            sFilePath = DefinePathInfo.GetSystemSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, sFilePath);
            oCache = new TSystemSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        public TDatabaseSettings GetDatabaseSettings()
        {
            return CacheFunc.GetDatabaseSettings();
        }

        /// <summary>
        /// 儲存資料庫設定。
        /// </summary>
        /// <param name="settings">資料庫設定。</param>
        public void SaveDatabaseSettings(TDatabaseSettings settings)
        {
            TDatabaseSettingsCache oCache;
            string sFilePath;

            // 儲存資料庫設定後，移除快取
            sFilePath = DefinePathInfo.GetDatabaseSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, sFilePath);
            oCache = new TDatabaseSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        public TProgramSettings GetProgramSettings()
        {
            return CacheFunc.GetProgramSettings();
        }

        /// <summary>
        /// 儲存程式清單。
        /// </summary>
        /// <param name="settings">程式清單。</param>
        public void SaveProgramSettings(TProgramSettings settings)
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
        public TDbSchemaSettings GetDbSchemaSettings()
        {
            return CacheFunc.GetDbSchemaSettings();
        }

        /// <summary>
        /// 儲存資料庫結構設定。
        /// </summary>
        /// <param name="settings">資料庫結構設定。</param>
        public void SaveDbSchemaSettings(TDbSchemaSettings settings)
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
        public TDbTable GetDbTable(string dbName, string tableName)
        {
            return CacheFunc.GetDbTable(dbName, tableName);
        }

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        public void SaveDbTable(string dbName, TDbTable dbTable)
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
        /// <param name="progID">程式代碼。</param>
        public TFormDefine GetFormDefine(string progID)
        {
            return CacheFunc.GetFormDefine(progID);
        }

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public void SaveFormDefine(TFormDefine formDefine)
        {
            TFormDefineCache oCache;

            // 儲存資料表結構後，移除快取
            BackendInfo.DefineProvider.SaveFormDefine(formDefine);
            oCache = new TFormDefineCache();
            oCache.Remove(formDefine.ProgID);
        }

        /// <summary>
        /// 取得表單排版。
        /// </summary>
        /// <param name="layoutID">版面代碼。</param>
        public TFormLayout GetFormLayout(string layoutID)
        {
            return CacheFunc.GetFormLayout(layoutID);
        }

        /// <summary>
        /// 儲存表單排版。
        /// </summary>
        /// <param name="formLayout">表單排版。</param>
        public void SaveFormLayout(TFormLayout formLayout)
        {
            TFormLayoutCache oCache;

            // 儲存表單版面配置後，移除快取
            BackendInfo.DefineProvider.SaveFormLayout(formLayout);
            oCache = new TFormLayoutCache();
            oCache.Remove(formLayout.LayoutID);
        }
    }
}
