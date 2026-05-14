namespace Bee.Db.Dml
{
    /// <summary>
    /// Defines the interface for building the SQL paging clause
    /// (LIMIT/OFFSET or OFFSET/FETCH) for the underlying dialect.
    /// </summary>
    public interface ILimitBuilder
    {
        /// <summary>
        /// Builds the dialect-specific paging clause. Returns an empty string when
        /// both <paramref name="skip"/> and <paramref name="take"/> are null.
        /// </summary>
        /// <param name="skip">Rows to skip; null means no offset.</param>
        /// <param name="take">Rows to take; null means no row limit.</param>
        string Build(int? skip, int? take);
    }
}
