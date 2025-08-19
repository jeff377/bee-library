using System.Data;
using System.Data.Common;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令輔助類別介面。
    /// </summary>
    public interface IDbCommandHelper
    {
        /// <summary>
        /// 資料庫命令。
        /// </summary>
        DbCommand DbCommand { get; }

        /// <summary>
        /// 參數符號。
        /// </summary>
        string ParameterSymbol { get; }

        /// <summary>
        /// 取得含前導字的參數名稱。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        string GetParameterName(string name);

        /// <summary>
        /// 在資料表或欄位名稱上加上適當的跳脫字元（Quoting Identifier）。
        /// </summary>
        /// <param name="identifier">資料表或欄位名稱。</param>
        /// <returns>回傳加上跳脫字元的識別字。</returns>
        string QuoteIdentifier(string identifier);

        /// <summary>
        /// 新增命令參數。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="dbType">資料型別。</param>
        /// <param name="value">參數值。</param>
        DbParameter AddParameter(string name, FieldDbType dbType, object value);

        /// <summary>
        /// 新增命令參數。
        /// </summary>
        /// <param name="field">結構欄位。</param>
        /// <param name="sourceVersion"> DataRow 取值版本。</param>
        DbParameter AddParameter(DbField field, DataRowVersion sourceVersion = DataRowVersion.Current);

        /// <summary>
        /// 設定資料庫命令字串。
        /// </summary>
        /// <param name="commandText">命令字串。</param>
        void SetCommandText(string commandText);

        /// <summary>
        /// 設定資料庫命令字串，並用命令參數集合做格式化字串。
        /// </summary>
        /// <param name="commandText">命令字串。</param>
        void SetCommandFormatText(string commandText);
    }
}
