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
    /// 資料庫命令輔助基底類別。
    /// </summary>
    public class TDbCommandHelper : IDbCommandHelper
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <param name="commandType">命令類型。</param>
        public TDbCommandHelper(EDatabaseType databaseType, CommandType commandType = CommandType.Text)
        {
            DatabaseType = databaseType;
            Provider = DbProviderManager.GetFactory(databaseType);
            DbCommand = Provider.CreateCommand();
            DbCommand.CommandType = commandType;
        }

        #endregion;

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public EDatabaseType DatabaseType { get; private set; }

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
        private static readonly Dictionary<EDatabaseType, string> ParameterSymbols = new Dictionary<EDatabaseType, string>
        {
            { EDatabaseType.SQLServer, "@" },
            { EDatabaseType.MySQL, "?" },
            { EDatabaseType.SQLite, "@" },
            { EDatabaseType.Oracle, ":" }
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
        private static readonly Dictionary<EDatabaseType, Func<string, string>> QuoteIdentifiers = new Dictionary<EDatabaseType, Func<string, string>>
        {
            { EDatabaseType.SQLServer, s => $"[{s}]" },
            { EDatabaseType.MySQL, s => $"`{s}`" },
            { EDatabaseType.SQLite, s => $"\"{s}\"" },
            { EDatabaseType.Oracle, s => $"\"{s}\"" }
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
        public DbParameter AddParameter(string name, EFieldDbType dbType, object value)
        {
            // 建立參數
            var parameter = Provider.CreateParameter();
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
        public DbParameter AddParameter(TDbField field, DataRowVersion sourceVersion = DataRowVersion.Current)
        {
            // 建立參數
            var parameter = Provider.CreateParameter();
            parameter.ParameterName = GetParameterName(field.FieldName);
            parameter.DbType = DbFunc.ConvertToDbType(field.DbType);
            parameter.SourceColumn = field.FieldName;
            parameter.SourceVersion = sourceVersion;
            if (!field.AllowNull)
                parameter.Value = DataSetFunc.GetDefaultValue(field.DbType);
            if (field.DbType == EFieldDbType.String)
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
            DbCommand.CommandText = commandText;
        }

        /// <summary>
        /// 設定資料庫命令字串，並用命令參數集合做格式化字串。
        /// </summary>
        /// <param name="commandText">命令字串。</param>
        public void SetCommandFormatText(string commandText)
        {
            if (StrFunc.Contains(commandText, CommandTextVariable.Parameters))
            {
                var sb = new StringBuilder();
                for (int N1 = 0; N1 < DbCommand.Parameters.Count; N1++)
                    StrFunc.Merge(sb, "{" + N1 + "}", ",");
                commandText = StrFunc.Replace(commandText, CommandTextVariable.Parameters, sb.ToString());
            }
            DbCommand.CommandText = DbFunc.SqlFormat(commandText, DbCommand.Parameters);
        }

        /// <summary>
        /// 取得執行命令的資料庫編號。
        /// </summary>
        /// <param name="databaseID">資料庫編號，則以 BackendInfo.DatabaseID 為主。</param>
        private string GetDatabaseID(string databaseID)
        {
            if (StrFunc.IsNotEmpty(databaseID)) { return databaseID; }
            if (StrFunc.IsEmpty(BackendInfo.DatabaseID))
                throw new TException("BackendInfo.DatabaseID is empty");
            return BackendInfo.DatabaseID;
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。 
        /// </summary>
        /// <param name="databaseID">資料庫編號，則以 BackendInfo.DatabaseID 為主。</param>
        public DataTable ExecuteDataTable(string databaseID = "")
        {
            string id = GetDatabaseID(databaseID);
            return SysDb.ExecuteDataTable(id, DbCommand);
        }

        /// <summary>
        /// 執行資料庫命令，傳回一筆資料列。 
        /// </summary>
        /// <param name="databaseID">資料庫編號，則以 BackendInfo.DatabaseID 為主。</param>
        public DataRow ExecuteDataRow(string databaseID = "")
        {
            var table = ExecuteDataTable(databaseID);
            if (BaseFunc.IsEmpty(table))
                return null;
            else
                return table.Rows[0];
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="databaseID">資料庫編號，則以 BackendInfo.DatabaseID 為主。</param>
        public int ExecuteNonQuery(string databaseID = "")
        {
            string id = GetDatabaseID(databaseID);
            return SysDb.ExecuteNonQuery(id, this.DbCommand);
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="databaseID">資料庫編號，則以 BackendInfo.DatabaseID 為主。</param>
        public object ExecuteScalar(string databaseID = "")
        {
            string id = GetDatabaseID(databaseID);
            return SysDb.ExecuteScalar(id, this.DbCommand);
        }

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="databaseID">
        /// 指定資料庫編號，若未提供，則使用 <c>BackendInfo.DatabaseID</c>。
        /// </param>
        /// <returns>
        /// 返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。
        /// </returns>
        public IEnumerable<T> Query<T>(string databaseID = "")
        {
            string id = GetDatabaseID(databaseID);
            // 使用 command 執行資料庫查詢，並取得 DbDataReader
            var reader = SysDb.ExecuteReader(id, this.DbCommand);
            var mapper = ILMapper<T>.CreateMapFunc(reader);
            // 延遲執行，不能使用 using，會造成連線被提早關閉
            try
            {
                foreach (var item in ILMapper<T>.MapToEnumerable(reader, mapper))
                {
                    yield return item;
                }
            }
            finally
            {
                reader.Dispose(); // 迭代結束後才關閉 reader
            }
        }
    }
}
