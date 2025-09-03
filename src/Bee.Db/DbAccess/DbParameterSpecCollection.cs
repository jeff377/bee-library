using Bee.Define;
using System;
using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫命令參數描述集合。
    /// </summary>
    public class DbParameterSpecCollection : KeyCollectionBase<DbParameterSpec>
    {
        /// <summary>
        /// 加入參數，根據物件值推斷 DbType。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="value">參數值。</param>
        public DbParameterSpec Add(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name));

            var parameter = new DbParameterSpec(name, value);
            Add(parameter);
            return parameter;
        }

    }
}
