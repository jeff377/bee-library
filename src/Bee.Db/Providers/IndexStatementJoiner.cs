using System.Text;
using Bee.Definition.Database;

namespace Bee.Db.Providers
{
    /// <summary>
    /// Joins the per-index CREATE statements for a table, shared by every dialect's create-table builder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the loop is shared, never the SQL: each builder passes its own <c>formatOne</c> and keeps
    /// full control of what an index statement looks like. What is shared is the rule the loop
    /// encodes — <b>the primary key is not emitted here</b>, because every dialect declares it as
    /// part of CREATE TABLE. All four builders had their own copy of that filter, and one of them
    /// spelled the method name <c>GetIndexsCommandText</c>, which is a fair sign of how much
    /// attention a copy attracts.
    /// </para>
    /// <para>
    /// A provider that lost the filter would emit a second, redundant index over the primary key
    /// columns — valid SQL on every dialect, so nothing would fail. That is the kind of divergence
    /// worth removing the opportunity for; the dialect-specific half stays where it belongs
    /// (<c>rules/database.md</c>).
    /// </para>
    /// </remarks>
    internal static class IndexStatementJoiner
    {
        /// <summary>
        /// Builds one statement per non-primary-key index, one per line.
        /// </summary>
        /// <param name="indexes">The table's indexes, primary key included; it is filtered out here.</param>
        /// <param name="tableName">The table name passed through to <paramref name="formatOne"/>.</param>
        /// <param name="formatOne">Produces the dialect's CREATE INDEX statement for one index.</param>
        /// <returns>The statements separated by newlines, with no trailing newline.</returns>
        public static string Join(
            IEnumerable<DbTableIndex>? indexes,
            string tableName,
            Func<string, DbTableIndex, string> formatOne)
        {
            ArgumentNullException.ThrowIfNull(formatOne);
            if (indexes is null) { return string.Empty; }

            var builder = new StringBuilder();
            foreach (var index in indexes)
            {
                if (index.PrimaryKey) { continue; }
                builder.AppendLine(formatOne(tableName, index));
            }
            // Trim so the block does not end with a stray newline.
            return builder.ToString().Trim();
        }
    }
}
