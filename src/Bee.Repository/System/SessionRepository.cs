using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Db;
using Bee.Definition;
using Bee.Repository.Abstractions.System;

namespace Bee.Repository.System
{
    /// <summary>
    /// Data access object for session information, encapsulating operations on the st_session and st_user tables.
    /// </summary>
    /// <remarks>
    /// This class is responsible for creating, querying, and deleting session user data, using <see cref="SessionUser"/> as the data model.
    /// Common use cases include generating an AccessToken on user login, validating session state, and clearing expired sessions.
    /// </remarks>
    public class SessionRepository : ISessionRepository
    {
        /// <summary>
        /// Inserts a session record into the database.
        /// </summary>
        /// <param name="sessionUser">The session user data to persist.</param>
        private static void Insert(SessionUser sessionUser)
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
        /// Deletes the session record for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        private static void Delete(Guid accessToken)
        {
            string sql = "DELETE FROM st_session \n" +
                                 "WHERE access_token={0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, accessToken);
            var dbAccess = new DbAccess(BackendInfo.DatabaseId);
            dbAccess.Execute(command);
        }

        /// <summary>
        /// Gets the session information for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SessionUser? GetSession(Guid accessToken)
        {
            string sql = "SELECT session_user_xml, sys_invalid_time \n" +
                                 "FROM st_session \n" +
                                 "WHERE access_token={0}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, accessToken);
            var dbAccess = new DbAccess(BackendInfo.DatabaseId);
            var result = dbAccess.Execute(command);
            var table = result.Table!;
            if (table.IsEmpty()) { return null; }
            var row = table.Rows[0];

            // If the session has expired, delete it and return null
            DateTime endTime = BaseFunc.CDateTime(row[SysFields.InvalidDate]);
            if (endTime < DateTime.UtcNow)
            {
                Delete(accessToken);
                return null;
            }

            string xml = BaseFunc.CStr(row["session_user_xml"]);
            var user = SerializeFunc.XmlToObject<SessionUser>(xml);
            // If the session is one-time use, delete it after retrieval
            if (user!.OneTime) { Delete(accessToken); }
            return user;
        }

        /// <summary>
        /// Creates a new user session.
        /// </summary>
        /// <param name="userID">The user account identifier.</param>
        /// <param name="expiresIn">The expiration time in seconds.</param>
        /// <param name="oneTime">Whether the session is valid for one-time use only.</param>
        public SessionUser CreateSession(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            string sql = "SELECT sys_id, sys_name FROM st_user \n" +
                                 "WHERE sys_id={0}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, userID);
            var dbAccess = new DbAccess(BackendInfo.DatabaseId);
            var result = dbAccess.Execute(command);
            var table = result.Table!;
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
            Insert(user);
            return user;
        }
    }
}
