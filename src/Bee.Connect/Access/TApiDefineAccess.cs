using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 透過 API 進行定義資料存取。
    /// </summary>
    public class TApiDefineAccess : IDefineAccess
    {
        private readonly TSystemApiConnector _connector = null;
        private readonly Dictionary<object> _list = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="connector">系統層級 API 服務連接器。</param>
        public TApiDefineAccess(TSystemApiConnector connector)
        {
            _connector = connector;
            _list = new Dictionary<object>();
        }

        #endregion

        /// <summary>
        /// 系統層級 API 服務連接器。
        /// </summary>
        private TSystemApiConnector Connector
        {
            get { return _connector; }
        }

        /// <summary>
        /// 存放已取得定義資料的集合。
        /// </summary>
        private Dictionary<object> List
        {
            get { return _list; }
        }

        /// <summary>
        /// 取得定義物件的快取鍵值。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">存取定義資料的鍵值。</param>
        private string GetCacheKey(DefineType defineType, string[] keys = null)
        {
            string cacheKey = $"{defineType}";
            if (keys != null && keys.Length > 0)
            {
                cacheKey += "_";
                foreach (string value in keys)
                    cacheKey += $".{value}";
            }
            return cacheKey;
        }

        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        private T GetDefine<T>(DefineType defineType, string[] keys = null)
        {
            object defineObject;
            string cacheKey = GetCacheKey(defineType, keys);
            if (this.List.ContainsKey(cacheKey))
            {
                // 若定義資料已存在，則直接回傳
                defineObject = this.List[cacheKey];
            }
            else
            {
                // 下載定義資料，並加入集合
                defineObject = this.Connector.GetDefine<T>(defineType, keys);
                this.List.Add(cacheKey, defineObject);
            }
            return (T)defineObject;
        }

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        private void SaveDefine(DefineType defineType, object defineObject, string[] keys = null)
        {
            this.Connector.SaveDefine(defineType, defineObject, keys);
        }

        /// <summary>
        /// 取得系統設定。
        /// </summary>
        /// <returns></returns>
        public SystemSettings GetSystemSettings()
        {
            return GetDefine<SystemSettings>(DefineType.SystemSettings);
        }

        /// <summary>
        /// 儲存系統設定。
        /// </summary>
        /// <param name="settings">系統設定。</param>
        public void SaveSystemSettings(SystemSettings settings)
        {
            SaveDefine(DefineType.SystemSettings, settings);
        }

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        public DatabaseSettings GetDatabaseSettings()
        {
            return GetDefine<DatabaseSettings>(DefineType.DatabaseSettings);
        }

        /// <summary>
        /// 儲存資料庫設定。
        /// </summary>
        /// <param name="settings">資料庫設定。</param>
        public void SaveDatabaseSettings(DatabaseSettings settings)
        {
            SaveDefine(DefineType.DatabaseSettings, settings);
        }

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        public ProgramSettings GetProgramSettings()
        {
            return GetDefine<ProgramSettings>(DefineType.ProgramSettings);
        }

        /// <summary>
        /// 儲存程式清單。
        /// </summary>
        /// <param name="settings">程式清單。</param>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            SaveDefine(DefineType.ProgramSettings, settings);
        }

        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        public DbSchemaSettings GetDbSchemaSettings()
        {
            return GetDefine<DbSchemaSettings>(DefineType.DbSchemaSettings);
        }

        /// <summary>
        /// 儲存資料庫結構設定。
        /// </summary>
        /// <param name="settings">資料庫結構設定。</param>
        public void SaveDbSchemaSettings(DbSchemaSettings settings)
        {
            SaveDefine(DefineType.DbSchemaSettings, settings);
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public DbTable GetDbTable(string dbName, string tableName)
        {
            return GetDefine<DbTable>(DefineType.DbTable, new string[] { dbName, tableName });
        }

        /// <summary>
        /// 儲存資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="dbTable">資料表結構。</param>
        public void SaveDbTable(string dbName, DbTable dbTable)
        {
            SaveDefine(DefineType.DbTable, dbTable, new string[] { dbName });
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public FormDefine GetFormDefine(string progId)
        {
            return GetDefine<FormDefine>(DefineType.FormDefine, new string[] { progId });
        }

        /// <summary>
        /// 儲存表單定義。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public void SaveFormDefine(FormDefine formDefine)
        {
            SaveDefine(DefineType.FormDefine, formDefine);
        }

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutId">排版代碼。</param>
        public FormLayout GetFormLayout(string layoutId)
        {
            return GetDefine<FormLayout>(DefineType.FormLayout, new string[] { layoutId });
        }

        /// <summary>
        /// 儲存表單版面配置。
        /// </summary>
        /// <param name="formLayout">表單版面配置。</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            SaveDefine(DefineType.FormLayout, formLayout);
        }
    }
}
