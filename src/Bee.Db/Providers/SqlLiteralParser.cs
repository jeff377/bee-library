namespace Bee.Db.Providers
{
    /// <summary>
    /// Parsing helpers for values read back from a database catalog, shared by the schema providers.
    /// </summary>
    /// <remarks>
    /// Dialect-neutral on purpose. Quoting a literal with <c>'</c> and doubling an embedded quote is
    /// SQL itself rather than any one provider's idea, and the PostgreSQL and Oracle schema providers
    /// held verbatim copies of the same eight lines. <c>rules/database.md</c> asks that providers keep
    /// room to diverge, and that applies to the SQL they <b>emit</b> — this is the other kind of code:
    /// reading a value back and undoing the quoting the catalog put on it.
    /// </remarks>
    internal static class SqlLiteralParser
    {
        /// <summary>
        /// Removes the surrounding single quotes from a catalog default value and unescapes the
        /// doubled quotes inside it.
        /// </summary>
        /// <param name="value">The raw catalog value, which may or may not be quoted.</param>
        /// <returns>The unquoted value, or <paramref name="value"/> when it carries no quotes.</returns>
        public static string StripStringLiteral(string value)
        {
            if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
            {
                string inner = value.Substring(1, value.Length - 2);
                return inner.Replace("''", "'");
            }
            return value;
        }
    }
}
