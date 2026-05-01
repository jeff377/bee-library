using Bee.Base;
using Bee.Base.Collections;
using Bee.Definition;
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
        private int _commandTimeout = DefaultTimeout;
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
        /// Gets or sets the command execution timeout in seconds.
        /// - 0 or negative → uses the default value of 30 seconds.
        /// - Greater than the global cap → the global cap is applied.
        /// - Any other positive value → used as-is.
        /// </summary>
        public int CommandTimeout
        {
            get => _commandTimeout;
            set
            {
                int cap = BackendInfo.MaxDbCommandTimeout;

                if (value <= 0)
                {
                    _commandTimeout = DefaultTimeout; // Default value
                }
                else
                {
                    _commandTimeout = (cap > 0 && value > cap) ? cap : value;
                }
            }
        }

        /// <summary>
        /// Gets the parameter specifications for this command.
        /// </summary>
        public DbParameterSpecCollection Parameters { get; } = [];

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
                var p = cmd.CreateParameter();
                p.ParameterName = (string.IsNullOrEmpty(parameterPrefix) || spec.Name.StartsWith(parameterPrefix))
                    ? spec.Name
                    : parameterPrefix + spec.Name;
                p.Value = NormalizeParameterValue(databaseType, spec.Value) ?? DBNull.Value;
                if (spec.DbType.HasValue) p.DbType = NormalizeDbType(databaseType, spec.DbType.Value);
                if (spec.Size.HasValue && spec.Size.Value > 0) p.Size = spec.Size.Value;
                p.IsNullable = spec.IsNullable;
                if (!string.IsNullOrEmpty(spec.SourceColumn))
                    p.SourceColumn = spec.SourceColumn;
                p.SourceVersion = spec.SourceVersion;
                cmd.Parameters.Add(p);
            }

            return cmd;
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
            return value;
        }

        /// <summary>
        /// Normalizes a parameter <see cref="DbType"/> for the target provider. Oracle.ManagedDataAccess
        /// rejects <c>DbType.Guid</c> on <see cref="System.Data.Common.DbParameter.DbType"/> assignment
        /// (Oracle has no native UUID type — the framework maps <see cref="Bee.Base.Data.FieldDbType.Guid"/>
        /// to <c>RAW(16)</c>); rewrite to <see cref="DbType.Binary"/> for Oracle. This pairs with
        /// <see cref="NormalizeParameterValue"/>, which converts the value side from <see cref="Guid"/>
        /// to <c>byte[]</c>.
        /// </summary>
        internal static DbType NormalizeDbType(DatabaseType databaseType, DbType dbType)
        {
            if (databaseType == DatabaseType.Oracle && dbType == DbType.Guid)
                return DbType.Binary;
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
            if (!StrFunc.Contains(commandText, CommandTextVariable.Parameters))
                return commandText;

            var sb = new StringBuilder();
            for (int i = 0; i < Parameters.Count; i++)
                StrFunc.Merge(sb, "{" + i + "}", ",");
            return StrFunc.Replace(commandText, CommandTextVariable.Parameters, sb.ToString());
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
