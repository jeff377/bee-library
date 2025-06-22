using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 連線資訊的資料存取物件，封裝存取 ts_session 與 ts_user 資料表的操作邏輯。
    /// </summary>
    /// <remarks>
    /// 此類別負責建立、查詢與刪除 Session 使用者資料，並以 <see cref="TSessionUser"/> 為資料模型。
    /// 常見用途包含使用者登入產生 AccessToken、驗證連線狀態、清除過期連線等情境。
    /// </remarks>
    public class TSessionRepository
    {
        /// <summary>
        /// 寫入連線資訊。
        /// </summary>
        /// <param name="sessionUser">連線資訊儲存的用戶資料。</param>
        private void Insert(TSessionUser sessionUser)
        {
            string xml = SerializeFunc.ObjectToXml(sessionUser);
            var helper = DbFunc.CreateDbCommandHelper();
            helper.AddParameter("access_token", EFieldDbType.Guid, sessionUser.AccessToken);
            helper.AddParameter("session_user_xml", EFieldDbType.Text, xml);
            helper.AddParameter(SysFields.InsertTime, EFieldDbType.DateTime, DateTime.Now);
            helper.AddParameter(SysFields.InvalidTime, EFieldDbType.DateTime, sessionUser.EndTime);
            string sql = "INSERT INTO ts_session \n" +
                                 "(access_token, session_user_xml, sys_insert_time, sys_invalid_time) \n" +
                                 "VALUES (" + CommandTextVariable.Parameters + ")";
            helper.SetCommandFormatText(sql);
            helper.ExecuteNonQuery();
        }

        /// <summary>
        /// 刪除連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        private void Delete(Guid accessToken)
        {
            var helper = DbFunc.CreateDbCommandHelper();
            helper.AddParameter("access_token", EFieldDbType.Guid, accessToken);
            string sql = "DELETE FROM ts_session \n" +
                                 "WHERE access_token={0}";
            helper.SetCommandFormatText(sql);
            helper.ExecuteNonQuery();
        }

        /// <summary>
        /// 取得連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TSessionUser GetSession(Guid accessToken)
        {
            var helper = DbFunc.CreateDbCommandHelper();
            helper.AddParameter("access_token", EFieldDbType.Guid, accessToken);
            string sql = "SELECT session_user_xml, sys_invalid_time \n" +
                                 "FROM ts_session \n" +
                                 "WHERE access_token={0}";
            helper.SetCommandFormatText(sql);
            var row = helper.ExecuteDataRow();
            if (row == null) { return null; }

            // 若連線已到期，刪除連線資訊，並回傳 null
            DateTime endTime = BaseFunc.CDateTime(row[SysFields.InvalidTime]);
            if (endTime < DateTime.Now)
            {
                this.Delete(accessToken);
                return null;
            }

            string xml = BaseFunc.CStr(row["session_user_xml"]);
            var user = SerializeFunc.XmlToObject<TSessionUser>(xml);
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
        public TSessionUser CreateSession(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            var helper = DbFunc.CreateDbCommandHelper();
            helper.AddParameter(SysFields.Id, EFieldDbType.String, userID);
            string sql = "SELECT sys_id, sys_name FROM ts_user \n" +
                                 "WHERE sys_id={0}";
            helper.SetCommandFormatText(sql);
            var row = helper.ExecuteDataRow();
            if (row == null) { throw new InvalidOperationException($"UserID='{userID}' not found"); }

            var user = new TSessionUser()
            {
                AccessToken = BaseFunc.NewGuid(),
                UserID = userID,
                UserName = BaseFunc.CStr(row[SysFields.Name]),
                EndTime = DateTime.Now.AddSeconds(expiresIn),
                OneTime = oneTime
            };
            this.Insert(user);
            return user;
        }
    }
}
