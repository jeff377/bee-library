namespace Bee.Db
{
    /// <summary>
    /// Creates <see cref="DbAccess"/> instances bound to the per-app configuration
    /// (such as the <see cref="System.Data.Common.DbCommand.CommandTimeout"/> cap).
    /// </summary>
    public interface IDbAccessFactory
    {
        /// <summary>
        /// Creates a <see cref="DbAccess"/> instance for the specified database identifier.
        /// </summary>
        /// <param name="databaseId">The database identifier (e.g. <c>common</c>, <c>company</c>).</param>
        DbAccess Create(string databaseId);
    }
}
