using Bee.Define;
using System;
using System.Data;
using System.Data.Common;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令描述，作為 <see cref="DbCommand"/> 的中介類別。
    /// </summary>
    public class DbCommandSpec
    {
        private const int DefaultTimeout = 30;  // 預設逾時秒數
        private int _commandTimeout = DefaultTimeout;

        /// <summary>
        /// 建構函式。
        /// </summary>
        public DbCommandSpec()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="kind">資料庫命令的執行種類。</param>
        /// <param name="commandText">要執行的 SQL 陳述式。</param>
        public DbCommandSpec(DbCommandKind kind, string commandText)
        {
            Kind = kind;
            CommandText = commandText;
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
        /// <param name="connection">資料庫連線，用於建立命令並自動綁定。</param>
        /// <param name="parameterPrefix">參數名稱的前綴符號（例如 SQL Server 為 <c>"@"</c>、Oracle 為 <c>":"</c>）。若為 <c>null</c> 或空字串，則不自動加上前綴。</param>
        public DbCommand CreateCommand(DbConnection connection, string parameterPrefix = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");
            if (string.IsNullOrWhiteSpace(CommandText))
                throw new InvalidOperationException("CommandText cannot be null or empty.");

            var cmd = connection.CreateCommand();
            cmd.CommandText = CommandText;
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


    }
}
