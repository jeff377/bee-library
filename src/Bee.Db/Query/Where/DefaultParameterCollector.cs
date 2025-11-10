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
