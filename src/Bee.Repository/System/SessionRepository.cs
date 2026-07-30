using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.Abstractions.System;
using Bee.Definition.Identity;

namespace Bee.Repository.System
{
    /// <summary>
    /// Data access object for the session seed stored in <c>st_session</c>, using
    /// <see cref="SessionUser"/> as the data model.
    /// </summary>
    /// <remarks>
    /// Writes are driven by the session lifecycle in <c>SystemBusinessObject</c>: sign-in inserts
    /// the seed, entering or leaving a company updates it, and sign-out deletes it.
    /// </remarks>
    public class SessionRepository : ISessionRepository
    {
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new <see cref="SessionRepository"/>.
        /// </summary>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public SessionRepository(IDbConnectionManager connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public void InsertSession(SessionUser sessionUser)
        {
            ArgumentNullException.ThrowIfNull(sessionUser);

            string xml = XmlCodec.Serialize(sessionUser);
            string sql = "INSERT INTO st_session \n" +
                                 "(access_token, session_user_xml, sys_insert_time, sys_invalid_time) \n" +
                                 "VALUES (" + CommandTextVariable.Parameters + ")";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, sessionUser.AccessToken, xml, DateTime.UtcNow, sessionUser.EndTime);
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            dbAccess.Execute(command);
        }

        /// <inheritdoc/>
        public void UpdateSession(SessionUser sessionUser)
        {
            ArgumentNullException.ThrowIfNull(sessionUser);

            // The whole seed is rewritten rather than one column patched: the caller holds the
            // current session and the seed lives inside a single XML column, so a read-modify-write
            // would cost an extra round trip to reach the same state.
            string xml = XmlCodec.Serialize(sessionUser);
            string sql = "UPDATE st_session \n" +
                                 "SET session_user_xml={1}, sys_invalid_time={2} \n" +
                                 "WHERE access_token={0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, sessionUser.AccessToken, xml, sessionUser.EndTime);
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            dbAccess.Execute(command);
        }

        /// <inheritdoc/>
        public void DeleteSession(Guid accessToken)
        {
            string sql = "DELETE FROM st_session \n" +
                                 "WHERE access_token={0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, accessToken);
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
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
            var dbAccess = new DbAccess(DbCategoryIds.Common, _connectionManager);
            var result = dbAccess.Execute(command);
            var table = result.Table!;
            if (table.IsEmpty()) { return null; }
            var row = table.Rows[0];

            // If the session has expired, delete it and return null.
            // The column is a naive one holding UTC (ADR-032 D1), and the write path stores
            // `SessionUser.EndTime`, itself computed from `DateTime.UtcNow`. Labelling it says so out loud: the comparison below is
            // correct either way — `DateTime` compares ticks and ignores `Kind` — so an unlabelled
            // value would leave the reader unable to tell a deliberate UTC basis from an oversight.
            DateTime endTime = DateTime.SpecifyKind(
                ValueUtilities.CDateTime(row["sys_invalid_time"], DateTime.MinValue), DateTimeKind.Utc);
            if (endTime < DateTime.UtcNow)
            {
                DeleteSession(accessToken);
                return null;
            }

            string xml = ValueUtilities.CStr(row["session_user_xml"]);
            var user = XmlCodec.Deserialize<SessionUser>(xml);
            // If the session is one-time use, delete it after retrieval
            if (user!.OneTime) { DeleteSession(accessToken); }
            return user;
        }
    }
}
