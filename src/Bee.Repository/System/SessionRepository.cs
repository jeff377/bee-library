using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Db;
using Bee.Definition;
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
    public class SessionRepository : RepositoryBase, ISessionRepository
    {
        /// <summary>
        /// Initializes a new <see cref="SessionRepository"/>.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        public SessionRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, DbScope.Common)
        {
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
            var dbAccess = CreateDbAccess();
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
            var dbAccess = CreateDbAccess();
            dbAccess.Execute(command);
        }

        /// <inheritdoc/>
        public void DeleteSession(Guid accessToken)
        {
            string sql = "DELETE FROM st_session \n" +
                                 "WHERE access_token={0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, accessToken);
            var dbAccess = CreateDbAccess();
            dbAccess.Execute(command);
        }

        /// <inheritdoc/>
        public int DeleteExpiredSessions()
        {
            string sql = "DELETE FROM st_session \n" +
                                 "WHERE sys_invalid_time <= {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, DateTime.UtcNow);
            var dbAccess = CreateDbAccess();
            var result = dbAccess.Execute(command);
            return result.RowsAffected;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// A pure read: expired rows are filtered by the query rather than deleted on the way past.
        /// This runs on the request path, potentially on several nodes at once, and a read that
        /// writes turns every session lookup into a statement that can contend or fail outright on
        /// a read-only replica. Reclaiming those rows belongs to the cleanup schedule; correctness
        /// does not depend on it, since an expired row can no longer be read back through here and
        /// `AccessTokenValidator` checks the expiry again regardless.
        ///
        /// `sys_invalid_time` is a naive column holding UTC (ADR-032 D1), matching the
        /// `DateTime.UtcNow`-based value the write path stores.
        /// </remarks>
        public SessionUser? GetSession(Guid accessToken)
        {
            string sql = "SELECT session_user_xml \n" +
                                 "FROM st_session \n" +
                                 "WHERE access_token={0} AND sys_invalid_time > {1}";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql, accessToken, DateTime.UtcNow);
            var dbAccess = CreateDbAccess();
            var result = dbAccess.Execute(command);
            var table = result.Table!;
            if (table.IsEmpty()) { return null; }

            string xml = ValueUtilities.CStr(table.Rows[0]["session_user_xml"]);
            return XmlCodec.Deserialize<SessionUser>(xml);
        }
    }
}
