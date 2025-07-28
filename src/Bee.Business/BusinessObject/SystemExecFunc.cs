using System;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 系統層級業務邏輯物件提供的自訂方法。
    /// </summary>
    internal class SystemExecFunc
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public SystemExecFunc(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        #endregion

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// Hello 測試方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        public void Hello(ExecFuncArgs args, ExecFuncResult result)
        {
            result.Parameters.Add("Hello", "Hello SystemObject");
        }

        /// <summary>
        /// 升級資料表結構。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        public void UpgradeTableSchema(ExecFuncArgs args, ExecFuncResult result)
        {
            string databaseId = args.Parameters.GetValue<string>("DatabaseId");
            string dbName = args.Parameters.GetValue<string>("DbName");
            string tableName = args.Parameters.GetValue<string>("TableName");
            var oBuilder = new TableSchemaBuilder(databaseId);
            bool upgraded = oBuilder.Execute(dbName, tableName);
            result.Parameters.Add("Upgraded", upgraded);  // 回傳是否已升級
        }

        /// <summary>
        /// 測試資料庫連線。
        /// </summary>
        public void TestConnection(ExecFuncArgs args, ExecFuncResult result)
        {
            var item = args.Parameters.GetValue<DatabaseItem>("DatabaseItem");
            var dbAccess = new DbAccess(item);
            dbAccess.TestConnection();
        }

        /// <summary>
        /// 指定 DatabaseId 測試資料庫連線。
        /// </summary>
        public void TestDatabaseId(ExecFuncArgs args, ExecFuncResult result)
        {
            string databaseId = args.Parameters.GetValue<string>("DatabaseId");
            var item = CacheFunc.GetDatabaseItem(databaseId);
            var dbAccess = new DbAccess(item);
            dbAccess.TestConnection();
        }

    }
}
