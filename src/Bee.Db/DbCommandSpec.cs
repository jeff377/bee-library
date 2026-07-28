using Bee.Base;
using Bee.Base.Collections;
using Bee.Base.Data;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using Bee.Definition.Database;

namespace Bee.Db
{
    /// <summary>
    /// Describes a database command as an intermediary for <see cref="DbCommand"/>.
    /// </summary>
    public class DbCommandSpec : CollectionItem
    {
        private const int DefaultTimeout = 30;  // Default timeout in seconds
        // Pre-compiled placeholder regex: {key}; supports {{key}} as an escape (outputs {key})
        private static readonly Regex PlaceholderRegex =
            new Regex(@"\{(?<key>[^\}]+)\}|\{\{(?<escaped>[^\}]+)\}\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Initializes a new empty instance of <see cref="DbCommandSpec"/>.
        /// </summary>
        public DbCommandSpec()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="DbCommandSpec"/> using positional parameters.
        /// </summary>
        /// <param name="kind">The execution kind of the database command.</param>
        /// <param name="commandText">The SQL statement to execute; use {0}, {1} positional placeholders.</param>
        /// <param name="values">Positional parameter values corresponding to {0}, {1}, ...</param>
        public DbCommandSpec(DbCommandKind kind, string commandText, params object[] values)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentNullException(nameof(commandText), "Command text cannot be null or empty.");

            Kind = kind;
            CommandText = commandText;

            if (values != null && values.Length > 0)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Parameters.Add("p" + i, values[i]);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DbCommandSpec"/> using named parameters.
        /// </summary>
        /// <param name="kind">The execution kind of the database command.</param>
        /// <param name="commandText">The SQL statement to execute; use {Name} named placeholders.</param>
        /// <param name="parameters">A dictionary of named parameter values.</param>
        public DbCommandSpec(DbCommandKind kind, string commandText, IDictionary<string, object>? parameters)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentNullException(nameof(commandText), "Command text cannot be null or empty.");

            Kind = kind;
            CommandText = commandText;

            if (parameters != null)
            {
                foreach (var kv in parameters)
                {
                    Parameters.Add(kv.Key, kv.Value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the execution kind of the database command.
        /// </summary>
        public DbCommandKind Kind { get; set; } = DbCommandKind.NonQuery;

        /// <summary>
        /// Gets or sets the SQL statement to execute.
        /// </summary>
        public string CommandText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command type; defaults to <see cref="CommandType.Text"/>.
        /// </summary>
        public CommandType CommandType { get; set; } = CommandType.Text;

        /// <summary>
        /// Gets or sets the command execution timeout in seconds. Stored as-is;
        /// the effective value applied to <see cref="DbCommand.CommandTimeout"/> is
        /// resolved by <see cref="DbAccess"/> at execution time (non-positive →
        /// default 30 sec; values exceeding the per-app cap are clamped).
        /// </summary>
        public int CommandTimeout { get; set; } = DefaultTimeout;

        /// <summary>
        /// Gets the parameter specifications for this command.
        /// </summary>
        public DbParameterSpecCollection Parameters { get; } = [];

        /// <summary>
        /// Gets or sets the columns of the result set that hold a calendar day rather than an instant.
        /// </summary>
        /// <remarks>
        /// ADO.NET reports a `date` column as `System.DateTime`, which loses the distinction the
        /// definition layer makes between <see cref="FieldDbType.Date"/> and
        /// <see cref="FieldDbType.DateTime"/>. Queries the framework generates from a schema are marked
        /// from that schema; a hand-written query has no schema to consult, so declare its calendar-day
        /// columns here and <see cref="DbAccess"/> marks them on the returned table.
        /// <para>
        /// Applies only to <see cref="DbCommandKind.DataTable"/>. Setting it on any other kind is
        /// rejected at execution time rather than ignored, because a declaration that silently does
        /// nothing is the failure this option exists to prevent. Column names are matched
        /// case-insensitively, and a name that matches no column raises an error.
        /// </para>
        /// </remarks>
        public List<string> DateColumns { get; } = [];

        /// <summary>
        /// Creates a <see cref="DbCommand"/> instance configured with the current <see cref="DbCommandSpec"/> settings.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        /// <param name="connection">The database connection used to create and bind the command.</param>
        public DbCommand CreateCommand(DatabaseType databaseType, DbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");
            if (string.IsNullOrWhiteSpace(CommandText))
                throw new InvalidOperationException("CommandText cannot be null or empty.");
            // Checked here rather than where the marks are applied, because a kind that returns no
            // table never reaches that code — the declaration would then be silently inert, which is
            // the failure `DateColumns` exists to prevent. Every execution path builds its command
            // through this method, so this is the one gate all of them pass.
            if (DateColumns.Count > 0 && Kind != DbCommandKind.DataTable)
            {
                throw new InvalidOperationException(
                    $"{nameof(DateColumns)} applies only to {nameof(DbCommandKind.DataTable)} commands; " +
                    $"this command is {Kind}.");
            }

            string parameterPrefix = databaseType.GetParameterPrefix();
            var cmd = connection.CreateCommand();
            // Pass through for StoredProcedure; skip parameter resolution
            cmd.CommandText = (CommandType == CommandType.StoredProcedure)
                ? CommandText
                : ResolveParameters(parameterPrefix);
            cmd.CommandType = CommandType;
            cmd.CommandTimeout = CommandTimeout;

            foreach (var spec in Parameters)
            {
                cmd.Parameters.Add(CreateParameter(cmd, databaseType, parameterPrefix, spec));
            }

            return cmd;
        }

        /// <summary>
        /// Builds a single provider parameter from its specification.
        /// </summary>
        /// <param name="cmd">The command the parameter is created from.</param>
        /// <param name="databaseType">The database type, which decides the value and type normalizations.</param>
        /// <param name="parameterPrefix">The provider's parameter prefix, prepended when the name lacks it.</param>
        /// <param name="spec">The parameter specification.</param>
        private static DbParameter CreateParameter(DbCommand cmd, DatabaseType databaseType,
            string parameterPrefix, DbParameterSpec spec)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = (string.IsNullOrEmpty(parameterPrefix) || spec.Name.StartsWith(parameterPrefix, StringComparison.Ordinal))
                ? spec.Name
                : parameterPrefix + spec.Name;
            p.Value = NormalizeParameterValue(databaseType, spec.Value) ?? DBNull.Value;
            // An inline `{0}` parameter carries no DbType, leaving the provider to infer one from
            // the CLR value — and Npgsql infers `timestamptz` for DateTime, which rejects
            // Kind=Unspecified outright and would silently apply the server's zone to a Utc one.
            // Naming DbType.DateTime pins it to the naive column type this framework stores in
            // (ADR-032 D1), and routes SQL Server through the datetime2 upgrade below so an
            // inline value keeps the same sub-millisecond precision a schema-driven one does.
            var dbType = spec.DbType ?? (spec.Value is DateTime ? DbType.DateTime : null);
            if (dbType.HasValue) p.DbType = NormalizeDbType(databaseType, dbType.Value);
            if (spec.Size.HasValue && spec.Size.Value > 0) p.Size = spec.Size.Value;
            p.IsNullable = spec.IsNullable;
            if (!string.IsNullOrEmpty(spec.SourceColumn))
                p.SourceColumn = spec.SourceColumn;
            p.SourceVersion = spec.SourceVersion;
            return p;
        }

        /// <summary>
        /// Normalizes a parameter value for the target provider. Oracle.ManagedDataAccess does
        /// not accept a <see cref="Guid"/> instance as the value of a <c>RAW(16)</c> parameter
        /// (the framework's <see cref="Bee.Base.Data.FieldDbType.Guid"/> mapping), and throws
        /// <c>ArgumentException : Value does not fall within the expected range</c>; convert to
        /// <c>byte[]</c> via <see cref="Guid.ToByteArray()"/> for Oracle only. All other providers
        /// pass the value through unchanged.
        /// </summary>
        internal static object? NormalizeParameterValue(DatabaseType databaseType, object? value)
        {
            if (databaseType == DatabaseType.Oracle && value is Guid guid)
                return guid.ToByteArray();
            // Npgsql validates DateTimeKind against the target PostgreSQL type and refuses both
            // mismatches: `timestamptz` demands Utc, `timestamp` demands anything but. Since every
            // DateTime parameter is sent as `timestamp` (see NormalizeDbType) and its value is
            // already UTC by ADR-032 D1, the Kind carries no information worth defending — D6 says
            // as much — so it is cleared here rather than making every caller remember to.
            if (databaseType == DatabaseType.PostgreSQL && value is DateTime dateTime)
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            return value;
        }

        /// <summary>
        /// Normalizes a parameter <see cref="DbType"/> for the target provider. Two provider-specific
        /// rewrites are applied:
        /// <list type="bullet">
        /// <item>Oracle.ManagedDataAccess rejects <c>DbType.Guid</c> on
        /// <see cref="System.Data.Common.DbParameter.DbType"/> assignment (Oracle has no native UUID type —
        /// the framework maps <see cref="Bee.Base.Data.FieldDbType.Guid"/> to <c>RAW(16)</c>); rewrite to
        /// <see cref="DbType.Binary"/> for Oracle. This pairs with <see cref="NormalizeParameterValue"/>,
        /// which converts the value side from <see cref="Guid"/> to <c>byte[]</c>.</item>
        /// <item>SQL Server upgrades <c>DbType.DateTime</c> to <c>DbType.DateTime2</c> so the full .NET
        /// <see cref="DateTime"/> range and 100 ns precision survive the parameter layer (plain
        /// <c>DbType.DateTime</c> rounds to ~3.33 ms and rejects pre-1753 values before transmission).
        /// Applied SQL Server-only so PostgreSQL/Oracle DateTime handling is unaffected.</item>
        /// </list>
        /// </summary>
        internal static DbType NormalizeDbType(DatabaseType databaseType, DbType dbType)
        {
            if (databaseType == DatabaseType.Oracle && dbType == DbType.Guid)
                return DbType.Binary;
            if (databaseType == DatabaseType.SQLServer && dbType == DbType.DateTime)
                return DbType.DateTime2;
            // Npgsql maps DbType.DateTime to `timestamptz`, which refuses a Kind=Unspecified value
            // outright and, for a Kind=Utc one, lets PostgreSQL re-express it in the server's zone on
            // the way into the `timestamp` column the schema actually declares. Either way the
            // database would be deciding the zone — the one thing ADR-032 D1 keeps it out of.
            // DbType.DateTime2 is Npgsql's spelling of `timestamp without time zone`.
            if (databaseType == DatabaseType.PostgreSQL && dbType == DbType.DateTime)
                return DbType.DateTime2;
            return dbType;
        }

        /// <summary>
        /// Resolves {0} or {Name} placeholders in CommandText and converts them to the database parameter format.
        /// Use {{Name}} to output a literal {Name}.
        /// </summary>
        /// <param name="parameterPrefix">The database parameter prefix character, e.g., @ or :.</param>
        /// <returns>The resolved SQL command text.</returns>
        private string ResolveParameters(string parameterPrefix)
        {
            if (string.IsNullOrWhiteSpace(CommandText))
                throw new InvalidOperationException("Failed to execute SQL command: Command text is empty.");

            string commandText = ExpandParametersVariable(CommandText);

            return PlaceholderRegex.Replace(commandText, match =>
            {
                var escaped = match.Groups["escaped"];
                if (escaped.Success)
                    return "{" + escaped.Value + "}";

                var key = match.Groups["key"].Value;
                var name = int.TryParse(key, out var index)
                    ? ResolveNumericKey(index)
                    : ResolveNamedKey(key);

                return string.IsNullOrEmpty(parameterPrefix) ? name : parameterPrefix + name;
            });
        }

        private string ExpandParametersVariable(string commandText)
        {
            if (!StringUtilities.Contains(commandText, CommandTextVariable.Parameters))
                return commandText;

            var sb = new StringBuilder();
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append('{').Append(i).Append('}');
            }
            return StringUtilities.Replace(commandText, CommandTextVariable.Parameters, sb.ToString());
        }

        private string ResolveNumericKey(int index)
        {
            if (index < 0 || index >= Parameters.Count)
                throw new InvalidOperationException(
                    $"Failed to resolve SQL parameter: Index {{{index}}} not found in Parameters collection.");

            var name = Parameters[index].Name;
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException(
                    $"Failed to resolve SQL parameter: Parameter at index {index} has empty name.");

            return name;
        }

        private string ResolveNamedKey(string key)
        {
            var param = Parameters.FirstOrDefault(p =>
                p.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Failed to resolve SQL parameter: Name {{{key}}} not found in Parameters collection.");

            if (string.IsNullOrWhiteSpace(param.Name))
                throw new InvalidOperationException(
                    $"Failed to resolve SQL parameter: Parameter '{key}' has empty name.");

            return param.Name;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return CommandText;
        }
    }
}
