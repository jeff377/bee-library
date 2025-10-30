using System;
using Bee.Base;
using Bee.Db;
using Bee.Define;
using Bee.Repository.Abstractions;

namespace Bee.Repository
{
    /// <summary>
    /// 連線資訊的資料存取物件，封裝存取 st_session 與 st_user 資料表的操作邏輯。
    /// </summary>
    /// <remarks>
    /// 此類別負責建立、查詢與刪除 Session 使用者資料，並以 <see cref="SessionUser"/> 為資料模型。
    /// 常見用途包含使用者登入產生 AccessToken、驗證連線狀態、清除過期連線等情境。
    /// </remarks>
    public class SessionRepository : ISessionRepository
    {
        /// <summary>
        /// 寫入連線資訊。
        /// </summary>
        /// <param name="sessionUser">連線資訊儲存的用戶資料。</param>
        private void Insert(SessionUser sessionUser)
        {
            string xml = SerializeFunc.ObjectToXml(sessionUser);
            string sql = "INSERT INTO st_session \n" +
                                 "(access_token, session_user_xml, sys_insert_time, sys_invalid_time) \n" +
                                 "VALUES (" + CommandTextVariable.Parameters + ")";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, sessionUser.AccessToken, xml, DateTime.Now, sessionUser.EndTime);
            var dbAccess = new DbAccess(BackendInfo.DatabaseId);
            dbAccess.Execute(command);
        }

        /// <summary>
        /// 刪除連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        private void Delete(Guid accessToken)
        {
            string sql = "DELETE FROM st_session \n" +
                                 "WHERE access_token={0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, accessToken);
            var dbAccess = new DbAccess(BackendInfo.DatabaseId);
            dbAccess.Execute(command);
        }

        /// <summary>
        /// 取得連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public SessionUser GetSession(Guid accessToken)
        {
            string sql = "SELECT session_user_xml, sys_invalid_time \n" +
                                 "FROM st_session \n" +
                                 "WHERE access_token={0}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, accessToken);
            var dbAccess = new DbAccess(BackendInfo.DatabaseId);
            var result = dbAccess.Execute(command);
            if (result.Table.IsEmpty()) { return null; }
            var row = result.Table.Rows[0];

            // 若連線已到期，刪除連線資訊，並回傳 null
            DateTime endTime = BaseFunc.CDateTime(row[SysFields.InvalidTime]);
            if (endTime < DateTime.Now)
            {
                this.Delete(accessToken);
                return null;
            }

            string xml = BaseFunc.CStr(row["session_user_xml"]);
            var user = SerializeFunc.XmlToObject<SessionUser>(xml);
            // 若為一次性有效，刪除連線資訊
            if (user.OneTime) { this.Delete(accessToken); }
            return user;
        }

        /// <summary>
        /// 建立一組用戶連線。
        /// </summary>
        /// <param name="userID">用戶帳號。</param>
        /// <param name="expiresIn">到期秒數。</param>
        /// <param name="oneTime">一次性有效。</param>
        public SessionUser CreateSession(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            string sql = "SELECT sys_id, sys_name FROM st_user \n" +
                                 "WHERE sys_id={0}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, userID);
            var dbAccess = new DbAccess(BackendInfo.DatabaseId);
            var result = dbAccess.Execute(command);
            var table = result.Table;
            if (table.IsEmpty()) { throw new InvalidOperationException($"UserID='{userID}' not found"); }
            var row = table.Rows[0];

            var user = new SessionUser()
            {
                AccessToken = BaseFunc.NewGuid(),
                UserID = userID,
                UserName = BaseFunc.CStr(row[SysFields.Name]),
                EndTime = DateTime.UtcNow.AddSeconds(expiresIn),
                OneTime = oneTime
            };
            this.Insert(user);
            return user;
        }
    }
}
