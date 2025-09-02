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
        /// 加入參數。
        /// </summary>
        /// <param name="name">參數名稱。</param>
        /// <param name="value">參數值。</param>
        public DbParameterSpec Add(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name));

            var parameter = new DbParameterSpec()
            {
                Name = name,
                Value = value,
                DbType = InferDbType(value)
            };
            Add(parameter);
            return parameter;
        }

        /// <summary>
        /// 根據物件值推斷 DbType。
        /// </summary>
        private static DbType? InferDbType(object value)
        {
            if (value == null || value is DBNull) return null;

            var type = value.GetType();

            if (type == typeof(string)) return DbType.String;
            if (type == typeof(int)) return DbType.Int32;
            if (type == typeof(long)) return DbType.Int64;
            if (type == typeof(short)) return DbType.Int16;
            if (type == typeof(byte)) return DbType.Byte;
            if (type == typeof(bool)) return DbType.Boolean;
            if (type == typeof(DateTime)) return DbType.DateTime;
            if (type == typeof(decimal)) return DbType.Decimal;
            if (type == typeof(double)) return DbType.Double;
            if (type == typeof(float)) return DbType.Single;
            if (type == typeof(Guid)) return DbType.Guid;
            if (type == typeof(byte[])) return DbType.Binary;
            if (type == typeof(TimeSpan)) return DbType.Time;

            // fallback：不指定，交給 Provider 自動判斷
            return null;
        }
    }
}
