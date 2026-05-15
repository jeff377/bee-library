using Bee.Definition;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// Resolves the physical databaseId a bo repo should use, given a logical
    /// <see cref="DbScope"/> and the current session's access token.
    /// </summary>
    /// <remarks>
    /// Routing rules:
    /// <list type="bullet">
    /// <item><see cref="DbScope.Common"/> → fixed databaseId <c>"common"</c>; does not require a session.</item>
    /// <item><see cref="DbScope.Log"/> → fixed databaseId <c>"log"</c>; does not require a session.</item>
    /// <item><see cref="DbScope.Company"/> → resolved via <c>SessionInfo.CompanyId</c>
    /// and <c>CompanyInfo.CompanyDatabaseId</c>.</item>
    /// </list>
    /// </remarks>
    public interface IRepositoryDatabaseRouter
    {
        /// <summary>
        /// Resolves the databaseId for the given scope and access token.
        /// </summary>
        /// <param name="scope">The bo repo's access intent.</param>
        /// <param name="accessToken">The current request's access token. Ignored for
        /// <see cref="DbScope.Common"/> and <see cref="DbScope.Log"/>; required for
        /// <see cref="DbScope.Company"/>.</param>
        /// <exception cref="UnauthorizedAccessException">
        /// <paramref name="scope"/> is <see cref="DbScope.Company"/> but the session
        /// cannot be found in the cache or has expired.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="scope"/> is <see cref="DbScope.Company"/> but the session
        /// has not entered a company, or the corresponding <c>CompanyInfo</c> is not
        /// available in the cache.
        /// </exception>
        string Resolve(DbScope scope, Guid accessToken);
    }
}
