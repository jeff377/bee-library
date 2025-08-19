using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令組裝輔助類別。
    /// </summary>
    public class DbCommandHelper
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <param name="commandType">命令類型。</param>
        public DbCommandHelper(DatabaseType databaseType, CommandType commandType = CommandType.Text)
        {
            DatabaseType = databaseType;
            Provider = DbProviderManager.GetFactory(databaseType)
                       ?? throw new InvalidOperationException($"Unknown database type: {databaseType}.");
            DbCommand = Provider.CreateCommand()
                       ?? throw new InvalidOperationException("DbProviderFactory.CreateCommand() returned null.");
            DbCommand.CommandType = commandType;
        }

        #endregion;

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public DatabaseType DatabaseType { get; private set; }

        /// <summary>
        /// 資料庫來源提供者。
        /// </summary>
        public DbProviderFactory Provider { get; private set; }

        /// <summary>
        /// 資料庫命令。
        /// </summary>
        public DbCommand DbCommand { get; protected set; }

        /// <summary>
        /// 參數符號字典。
        /// </summary>
        private static readonly Dictionary<DatabaseType, string> ParameterSymbols = new Dictionary<DatabaseType, string>
        {
            { DatabaseType.SQLServer, "@" },
            { DatabaseType.MySQL, "@" },
            { DatabaseType.SQLite, "@" },
            { DatabaseType.Oracle, ":" }
        };

        /// <summary>
        /// 參數符號。
        /// </summary>
        public string ParameterSymbol
        {
            get
            {
                return ParameterSymbols.TryGetValue(DatabaseType, out var symbol) ? symbol : "@";
            }
        }

        /// <summary>
        /// 取得含參數符號的參數名稱。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        public string GetParameterName(string name)
        {
            if (StrFunc.LeftWith(name, ParameterSymbol))
                return name;
            else
                return ParameterSymbol + name;
        }

        /// <summary>
        /// 跳脫字元字典。
        /// </summary>
        private static readonly Dictionary<DatabaseType, Func<string, string>> QuoteIdentifiers = new Dictionary<DatabaseType, Func<string, string>>
        {
            { DatabaseType.SQLServer, s => $"[{s}]" },
            { DatabaseType.MySQL, s => $"`{s}`" },
            { DatabaseType.SQLite, s => $"\"{s}\"" },
            { DatabaseType.Oracle, s => $"\"{s}\"" }
        };

        /// <summary>
        /// 在資料表或欄位名稱上加上適當的跳脫字元（Quoting Identifier）。
        /// </summary>
        /// <param name="identifier">資料表或欄位名稱。</param>
        /// <returns>回傳加上跳脫字元的識別字。</returns>
        public string QuoteIdentifier(string identifier)
        {
            return QuoteIdentifiers.TryGetValue(DatabaseType, out var quoteFunc) ? quoteFunc(identifier) : identifier;
        }

        /// <summary>
        /// 新增命令參數。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="dbType">資料型別。</param>
        /// <param name="value">參數值。</param>
        public DbParameter AddParameter(string name, FieldDbType dbType, object value)
        {
            // 建立參數
            var parameter = Provider.CreateParameter()
                           ?? throw new InvalidOperationException("DbProviderFactory.CreateParameter() returned null.");
            parameter.ParameterName = GetParameterName(name);
            parameter.DbType = DbFunc.ConvertToDbType(dbType);
            parameter.Value = BaseFunc.CDbFieldValue(dbType, value);
            // 加入參數
            DbCommand.Parameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// 新增命令參數。
        /// </summary>
        /// <param name="field">結構欄位。</param>
        /// <param name="sourceVersion"> DataRow 取值版本。</param>
        public DbParameter AddParameter(DbField field, DataRowVersion sourceVersion = DataRowVersion.Current)
        {
            // 建立參數
            var parameter = Provider.CreateParameter()
                           ?? throw new InvalidOperationException("DbProviderFactory.CreateParameter() returned null.");
            parameter.ParameterName = GetParameterName(field.FieldName);
            parameter.DbType = DbFunc.ConvertToDbType(field.DbType);
            parameter.SourceColumn = field.FieldName;
            parameter.SourceVersion = sourceVersion;
            if (!field.AllowNull)
                parameter.Value = DataSetFunc.GetDefaultValue(field.DbType);
            if (field.DbType == FieldDbType.String)
                parameter.Size = field.Length;
            // 加入參數
            DbCommand.Parameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// 設定資料庫命令字串。
        /// </summary>
        /// <param name="commandText">命令字串。</param>
        public void SetCommandText(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("commandText cannot be null or empty.", nameof(commandText));

            DbCommand.CommandText = commandText;
        }

        /// <summary>
        /// 設定資料庫命令字串，並用命令參數集合做格式化字串。
        /// </summary>
        /// <param name="commandText">命令字串。</param>
        public void SetCommandFormatText(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("commandText cannot be null or empty.", nameof(commandText));

            if (StrFunc.Contains(commandText, CommandTextVariable.Parameters))
            {
                var sb = new StringBuilder();
                for (int N1 = 0; N1 < DbCommand.Parameters.Count; N1++)
                    StrFunc.Merge(sb, "{" + N1 + "}", ",");
                commandText = StrFunc.Replace(commandText, CommandTextVariable.Parameters, sb.ToString());
            }
            DbCommand.CommandText = DbFunc.SqlFormat(commandText, DbCommand.Parameters);
        }
    }
}
