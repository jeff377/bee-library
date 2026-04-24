using System.Globalization;
using System.Text;
using Bee.Db.Schema;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// Generates metadata-only SQL (sp_addextendedproperty / sp_updateextendedproperty) for SQL Server
    /// description drift, independent of the CREATE / rebuild DDL path.
    /// </summary>
    public static class SqlExtendedPropertyCommandBuilder
    {
        /// <summary>
        /// Escapes a string value for use inside an N'...' literal by doubling single quotes.
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        private static string EscapeSqlString(string value)
        {
            return value.Replace("'", "''");
        }

        /// <summary>
        /// Gets the SQL that synchronizes MS_Description extended properties on the specified table.
        /// Returns an empty string if <paramref name="changes"/> is null or empty.
        /// </summary>
        /// <param name="tableName">The target table name.</param>
        /// <param name="changes">The description changes to apply.</param>
        public static string GetCommandText(string tableName, IReadOnlyList<DescriptionChange>? changes)
        {
            if (changes == null || changes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"-- Sync extended properties for {tableName}\r\n");
            foreach (var change in changes)
            {
                sb.AppendLine(GetExtendedPropertyCommand(tableName, change));
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Builds a single sp_addextendedproperty / sp_updateextendedproperty statement.
        /// </summary>
        private static string GetExtendedPropertyCommand(string tableName, DescriptionChange change)
        {
            string procedure = change.IsNew ? "sp_addextendedproperty" : "sp_updateextendedproperty";
            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"EXEC {procedure}\r\n");
            sb.Append(CultureInfo.InvariantCulture, $"  @name=N'MS_Description', @value=N'{EscapeSqlString(change.NewValue)}',\r\n");
            sb.Append("  @level0type=N'SCHEMA', @level0name=N'dbo',\r\n");
            sb.Append(CultureInfo.InvariantCulture, $"  @level1type=N'TABLE', @level1name=N'{EscapeSqlString(tableName)}'");
            if (change.Level == DescriptionLevel.Column)
            {
                sb.Append(CultureInfo.InvariantCulture, $",\r\n  @level2type=N'COLUMN', @level2name=N'{EscapeSqlString(change.FieldName)}'");
            }
            sb.Append(';');
            return sb.ToString();
        }
    }
}
