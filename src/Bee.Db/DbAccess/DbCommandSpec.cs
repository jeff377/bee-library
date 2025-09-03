using Bee.Define;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令描述，作為 <see cref="DbCommand"/> 的中介類別。
    /// </summary>
    public class DbCommandSpec
    {
        private const int DefaultTimeout = 30;  // 預設逾時秒數
        private int _commandTimeout = DefaultTimeout;
        // 預編譯：{key}；支援用 {{key}} 代表逃脫（輸出 {key}）
        private static readonly Regex PlaceholderRegex =
            new Regex(@"\{(?<key>[^\}]+)\}|\{\{(?<escaped>[^\}]+)\}\}", RegexOptions.Compiled);

        /// <summary>
        /// 建構函式。
        /// </summary>
        public DbCommandSpec()
        { }

        /// <summary>
        /// 建構函式（位置參數模式）。
        /// </summary>
        /// <param name="kind">資料庫命令的執行種類。</param>
        /// <param name="commandText">要執行的 SQL 陳述式，只能使用 {0}, {1} 格式。</param>
        /// <param name="values">位置參數值，依序對應 {0}, {1} ...</param>
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
        /// 建構函式（具名參數模式）。
        /// </summary>
        /// <param name="kind">資料庫命令的執行種類。</param>
        /// <param name="commandText">要執行的 SQL 陳述式，可使用 {Name} 格式。</param>
        /// <param name="parameters">具名參數集合。</param>
        public DbCommandSpec(DbCommandKind kind, string commandText, IDictionary<string, object> parameters)
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
        /// 資料庫命令的執行種類。
        /// </summary>
        public DbCommandKind Kind { get; set; } = DbCommandKind.NonQuery;

        /// <summary>
        /// 要執行的 SQL 陳述式。
        /// </summary>
        public string CommandText { get; set; } = string.Empty;

        /// <summary>
        /// 命令的類型，預設為 <see cref="CommandType.Text"/>。
        /// </summary>
        public CommandType CommandType { get; set; } = CommandType.Text;

        /// <summary>
        /// 執行命令的逾時（秒）。
        /// - 小於等於 0 → 使用預設值 30 秒。
        /// - 大於全域上限 → 套用全域上限。
        /// - 其他正值 → 直接採用。
        /// </summary>
        public int CommandTimeout
        {
            get => _commandTimeout;
            set
            {
                int cap = BackendInfo.MaxDbCommandTimeout;

                if (value <= 0)
                {
                    _commandTimeout = DefaultTimeout; // 預設值
                }
                else
                {
                    _commandTimeout = (cap > 0 && value > cap) ? cap : value;
                }
            }
        }

        /// <summary>
        /// 參數集合。
        /// </summary>
        public DbParameterSpecCollection Parameters { get; } = new DbParameterSpecCollection();

        /// <summary>
        /// 建立 <see cref="DbCommand"/> 實例，並依據目前的 <see cref="DbCommandSpec"/> 設定套用屬性與參數。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <param name="connection">資料庫連線，用於建立命令並自動綁定。</param>
        public DbCommand CreateCommand(DatabaseType databaseType, DbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");
            if (string.IsNullOrWhiteSpace(CommandText))
                throw new InvalidOperationException("CommandText cannot be null or empty.");

            string parameterPrefix = DbFunc.GetParameterPrefix(databaseType);
            var cmd = connection.CreateCommand();
            // StoredProcedure 直通，不做參數解析
            cmd.CommandText = (CommandType == CommandType.StoredProcedure)
                ? CommandText
                : ResolveParameters(parameterPrefix);
            cmd.CommandType = CommandType;
            cmd.CommandTimeout = CommandTimeout;

            foreach (var spec in Parameters)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = string.IsNullOrEmpty(parameterPrefix) ? spec.Name : parameterPrefix + spec.Name;
                p.Value = spec.Value ?? DBNull.Value;
                if (spec.DbType.HasValue) p.DbType = spec.DbType.Value;
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
        /// 解析 CommandText 中的 {0} 或 {Name}，並轉換成資料庫參數格式。
        /// 支援 {{Name}} 以輸出字面量 {Name}。
        /// </summary>
        /// <param name="parameterPrefix">資料庫參數前綴字元，例如 @ 或 :。</param>
        /// <returns>轉換後的 SQL 指令。</returns>
        private string ResolveParameters(string parameterPrefix)
        {
            if (string.IsNullOrWhiteSpace(CommandText))
                throw new InvalidOperationException("Failed to execute SQL command: Command text is empty.");

            return PlaceholderRegex.Replace(CommandText, match =>
            {
                // 字面量 {{...}} → 還原成 {...}
                var escaped = match.Groups["escaped"];
                if (escaped.Success)
                    return "{" + escaped.Value + "}";

                var key = match.Groups["key"].Value;

                // 數字 → 位置參數
                if (int.TryParse(key, out var index))
                {
                    if (index < 0 || index >= Parameters.Count)
                        throw new InvalidOperationException(
                            $"Failed to resolve SQL parameter: Index {{{index}}} not found in Parameters collection.");

                    var name = Parameters[index].Name;
                    if (string.IsNullOrWhiteSpace(name))
                        throw new InvalidOperationException(
                            $"Failed to resolve SQL parameter: Parameter at index {index} has empty name.");

                    return string.IsNullOrEmpty(parameterPrefix) ? name : parameterPrefix + name;
                }

                // 文字 → 具名參數（大小寫不敏感）
                var param = Parameters.FirstOrDefault(p =>
                    p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (param == null)
                    throw new InvalidOperationException(
                        $"Failed to resolve SQL parameter: Name {{{key}}} not found in Parameters collection.");

                if (string.IsNullOrWhiteSpace(param.Name))
                    throw new InvalidOperationException(
                        $"Failed to resolve SQL parameter: Parameter '{key}' has empty name.");

                return string.IsNullOrEmpty(parameterPrefix) ? param.Name : parameterPrefix + param.Name;
            });
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return CommandText;
        }
    }
}
