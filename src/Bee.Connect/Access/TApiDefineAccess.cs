using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 透過 API 進行定義資料存取。
    /// </summary>
    public class TApiDefineAccess : IDefineAccess
    {
        private readonly TSystemConnector _Connector = null;
        private readonly TDictionary<object> _List = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="connector">系統層級服務連線器。</param>
        public TApiDefineAccess(TSystemConnector connector)
        {
            _Connector = connector;
            _List = new TDictionary<object>();
        }

        #endregion

        /// <summary>
        /// 系統層級服務連線器。
        /// </summary>
        public TSystemConnector Connector
        {
            get { return _Connector; }
        }

        /// <summary>
        /// 存放已取得定義資料的集合。
        /// </summary>
        public TDictionary<object> List
        {
            get { return _List; }
        }

        /// <summary>
        /// 取得定義物件的識別鍵值
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">存取定義資料的鍵值。</param>
        private string GetKey(EDefineType defineType, string[] keys = null)
        {
            string sKey;

            sKey = $"{defineType}";
            if (keys != null && keys.Length > 0)
            {
                sKey += "_";
                foreach (string value in keys)
                    sKey += $".{value}";
            }
            return sKey;
        }

        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        private T GetDefine<T>(EDefineType defineType, string[] keys = null)
        {
            object oValue;
            string sKey;

            sKey = GetKey(defineType, keys);
            if (this.List.ContainsKey(sKey))
            {
                // 若定義資料已下載，則直接回傳
                oValue = this.List[sKey];
            }
            else
            {
                // 下載定義資料，並加入集合
                oValue = this.Connector.GetDefine<T>(defineType, keys);
                this.List.Add(sKey, oValue);
            }
            return (T)oValue;
        }

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        private void SaveDefine(EDefineType defineType, object defineObject, string[] keys = null)
        {
            this.Connector.SaveDefine(defineType, defineObject, keys);
        }

        /// <summary>
        /// 取得系統設定。
        /// </summary>
        /// <returns></returns>
        public TSystemSettings GetSystemSettings()
        {
            return GetDefine<TSystemSettings>(EDefineType.SystemSettings);
        }

        /// <summary>
        /// 儲存系統設定。
        /// </summary>
        /// <param name="settings">系統設定。</param>
        public void SaveSystemSettings(TSystemSettings settings)
        {
            SaveDefine(EDefineType.SystemSettings, settings);
        }

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        public TDatabaseSettings GetDatabaseSettings()
        {
            return GetDefine<TDatabaseSettings>(EDefineType.DatabaseSettings);
        }

        /// <summary>
        /// 儲存資料庫設定。
        /// </summary>
        /// <param name="settings">資料庫設定。</param>
        public void SaveDatabaseSettings(TDatabaseSettings settings)
        {
            SaveDefine(EDefineType.DatabaseSettings, settings);
        }

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        public TProgramSettings GetProgramSettings()
        {
            return GetDefine<TProgramSettings>(EDefineType.ProgramSettings);
        }

        /// <summary>
        /// 儲存程式清單。
        /// </summary>
        /// <param name="settings">程式清單。</param>
        public void SaveProgramSettings(TProgramSettings settings)
        {
            SaveDefine(EDefineType.ProgramSettings, settings);
        }

        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        public TDbSchemaSettings GetDbSchemaSettings()
        {
            return GetDefine<TDbSchemaSettings>(EDefineType.DbSchemaSettings);
        }

        /// <summary>
        /// 儲存資料庫結構設定。
        /// </summary>
        /// <param name="settings">資料庫結構設定。</param>
        public void SaveDbSchemaSettings(TDbSchemaSettings settings)
        {
            SaveDefine(EDefineType.DbSchemaSettings, settings);
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public TDbTable GetDbTable(string dbName, string tableName)
        {
            return GetDefine<TDbTable>(EDefineType.DbTable, new string[] { dbName, tableName });
        }

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        public void SaveDbTable(string dbName, TDbTable dbTable)
        {
            SaveDefine(EDefineType.DbTable, dbTable, new string[] { dbName });
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        public TFormDefine GetFormDefine(string progID)
        {
            return GetDefine<TFormDefine>(EDefineType.FormDefine, new string[] { progID });
        }

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public void SaveFormDefine(TFormDefine formDefine)
        {
            SaveDefine(EDefineType.FormDefine, formDefine);
        }

        /// <summary>
        /// 取得表單排版。
        /// </summary>
        /// <param name="layoutID">排版代碼。</param>
        public TFormLayout GetFormLayout(string layoutID)
        {
            return GetDefine<TFormLayout>(EDefineType.FormLayout, new string[] { layoutID });
        }

        /// <summary>
        /// 儲存表單排版。
        /// </summary>
        /// <param name="formLayout">表單排版。</param>
        public void SaveFormLayout(TFormLayout formLayout)
        {
            SaveDefine(EDefineType.FormLayout, formLayout);
        }
    }
}
