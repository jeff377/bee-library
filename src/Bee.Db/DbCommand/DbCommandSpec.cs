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
        /// <summary>
        /// 資料庫命令的執行種類。
        /// </summary>
        public DbCommandKind Kind { get; set; } = DbCommandKind.NonQuery;

        /// <summary>
        /// 要執行的 SQL 陳述式或儲存過程名稱。
        /// </summary>
        public string CommandText { get; set; } = string.Empty;

        /// <summary>
        /// 命令的類型，預設為 <see cref="CommandType.Text"/>。
        /// </summary>
        public CommandType CommandType { get; set; } = CommandType.Text;

        /// <summary>
        /// 執行命令的逾時（秒）。
        /// </summary>
        public int? CommandTimeout { get; set; }

        /// <summary>
        /// 參數集合。
        /// </summary>
        public DbParameterSpecCollection Parameters { get; } = new DbParameterSpecCollection();

        /// <summary>
        /// 建立 <see cref="DbCommand"/> 實例，並依據目前的 <see cref="DbCommandSpec"/> 設定套用屬性與參數。
        /// </summary>
        /// <param name="factory">提供資料庫命令與參數建立功能的 <see cref="DbProviderFactory"/>。</param>
        /// <param name="parameterPrefix">參數名稱的前綴符號（例如 SQL Server 為 <c>"@"</c>、Oracle 為 <c>":"</c>）。若為 <c>null</c> 或空字串，則不自動加上前綴。</param>
        public DbCommand CreateCommand(DbProviderFactory factory, string parameterPrefix = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory), "Factory cannot be null.");
            if (string.IsNullOrWhiteSpace(CommandText))
                throw new InvalidOperationException("CommandText cannot be null or empty.");

            var cmd = factory.CreateCommand()
                      ?? throw new InvalidOperationException("DbProviderFactory.CreateCommand() returned null.");

            cmd.CommandText = CommandText;
            cmd.CommandType = CommandType;
            if (CommandTimeout.HasValue) cmd.CommandTimeout = CommandTimeout.Value;

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
