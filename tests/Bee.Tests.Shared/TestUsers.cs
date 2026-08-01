using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 在 common 庫建立 / 移除測試專屬的 <c>st_user</c> 列。
    /// </summary>
    /// <remarks>
    /// WARNING: 會改寫使用者列的測試**不要動 seed 使用者 '001'** —— 實體資料庫由多個平行測試
    /// 行程共用，共用同一列必然競賽（已實際踩過：旗標測試與 BO 測試同時改 '001' 時，
    /// 另一邊讀到對方寫入的值）。每個測試建自己的列，測完刪掉。
    /// </remarks>
    public static class TestUsers
    {
        /// <summary>
        /// 建立一個唯一帳號的使用者列，回傳其 <c>sys_id</c>。
        /// </summary>
        /// <param name="connectionManager">連線管理員。</param>
        /// <param name="prefix">帳號前綴，用來在資料庫裡辨識來源測試。</param>
        public static string Create(IDbConnectionManager connectionManager, string prefix)
        {
            var dbType = connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string tbl = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier(SysFields.RowId);
            string colId = dbType.QuoteIdentifier(SysFields.Id);
            string colName = dbType.QuoteIdentifier(SysFields.Name);
            string colPwd = dbType.QuoteIdentifier("password");
            string colEmail = dbType.QuoteIdentifier("email");
            string colNote = dbType.QuoteIdentifier("note");
            string colTimeZone = dbType.QuoteIdentifier("time_zone");
            string colCulture = dbType.QuoteIdentifier("culture");

            // password / email / note 給單一空白而非空字串：Oracle 把 '' 視為 NULL，會撞上
            // NOT NULL constraint；其他 DB 仍是一字元字串。deployment_admin 與 sys_insert_time
            // 刻意不給值，讓欄位的 DEFAULT 生效——這正是「新使用者預設不是管理員」要驗的行為。
            string sysId = $"{prefix}-{Guid.NewGuid():N}"[..20];
            string sql = $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colPwd}, {colEmail}, {colNote}, {colTimeZone}, {colCulture}) " +
                         $"VALUES ({{0}}, {{1}}, {{2}}, ' ', ' ', ' ', 'Asia/Taipei', 'zh-TW')";
            new DbAccess(DbCategoryIds.Common, connectionManager)
                .Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, Guid.NewGuid(), sysId, "測試使用者"));
            return sysId;
        }

        /// <summary>
        /// 移除指定的使用者列。清理用，找不到列不視為錯誤。
        /// </summary>
        /// <param name="connectionManager">連線管理員。</param>
        /// <param name="sysId">要移除的使用者帳號。</param>
        public static void Delete(IDbConnectionManager connectionManager, string sysId)
        {
            var dbType = connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string sql = $"DELETE FROM {dbType.QuoteIdentifier("st_user")} " +
                         $"WHERE {dbType.QuoteIdentifier(SysFields.Id)} = {{0}}";
            new DbAccess(DbCategoryIds.Common, connectionManager)
                .Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, sysId));
        }
    }
}
