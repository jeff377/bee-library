using Bee.Define;
using System;
using System.Data;
using System.Data.Common;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令參數描述，作為 <see cref="DbParameter"/> 的中介類別。
    /// </summary>
    public class DbParameterSpec : KeyCollectionItem
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public DbParameterSpec()
        { }

        /// <summary>
        /// 建構函式，根據參數值推斷 DbType。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="value">參數值。</param>
        public DbParameterSpec(string name, object value)
        {
            Name= name;
            Value= value;
            DbType = DbFunc.InferDbType(value);
        }

        /// <summary>
        /// 參數名稱，不需包含參數前綴符號（例如 SQL Server 的 @）。
        /// </summary>
        public string Name
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 參數值，允許設定為 <c>null</c>。
        /// </summary>
        public object Value { get; set; } = null;

        /// <summary>
        /// 參數的資料型別。若為 <c>null</c>，則由資料庫提供者自動推斷。
        /// </summary>
        public DbType? DbType { get; set; }

        /// <summary>
        /// 參數的長度（適用於字串或二進位資料）。非必要時可不指定。
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// 指定是否允許參數值為 <see cref="DBNull"/>。
        /// </summary>
        public bool IsNullable { get; set; } = false;

        /// <summary>
        /// 對應來源 <see cref="DataRow"/> 中的欄位名稱，用於資料繫結與更新操作。
        /// </summary>
        public string SourceColumn { get; set; } = string.Empty;

        /// <summary>
        /// 指定來源資料列的版本，決定更新命令時取用的值。
        /// 例如：<see cref="DataRowVersion.Current"/>、<see cref="DataRowVersion.Original"/>、<see cref="DataRowVersion.Proposed"/>。
        /// </summary>
        public DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }
}
