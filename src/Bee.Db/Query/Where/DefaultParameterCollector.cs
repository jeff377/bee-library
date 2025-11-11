using System;
using System.Collections.Generic;

namespace Bee.Db
{
    /// <summary>
    /// 預設參數收集器，以 @p0, @p1, ... 生成參數名（或自訂前綴）。
    /// </summary>
    public sealed class DefaultParameterCollector : IParameterCollector
    {
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private int _index;

        /// <summary>
        /// 參數前綴，SQL Server 使用 '@'，Oracle 可使用 ':'。
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="prefix"></param>
        public DefaultParameterCollector(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentException("Prefix cannot be null or empty.", "prefix");
            }
            Prefix = prefix;
        }

        /// <inheritdoc/>
        /// <summary>
        /// 加入一個參數值，回傳參數名稱（含前綴符號）。
        /// </summary>
        /// <param name="value">要加入的參數值。</param>
        /// <returns>生成的參數名稱，格式為「前綴 + 'p' + 索引」（例如：@p0, @p1）。</returns>
        public string Add(object value)
        {
            var name = Prefix + "p" + _index.ToString();
            _index++;
            _parameters[name] = value;
            return name;
        }

        /// <inheritdoc/>
        public IDictionary<string, object> GetAll()
        {
            return _parameters;
        }
    }
}
