using System;
using System.Collections.Generic;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 的 ORDER BY 子句建置器
    /// </summary>
    public sealed class SqlServerSortBuilder : ISortBuilder
    {
        /// <summary>
        /// 根據指定的排序項目集合，產生 SQL 的 ORDER BY 子句（包含前綴關鍵字）。
        /// </summary>
        /// <param name="sorts">排序項目集合。</param>
        public string Build(SortItemCollection sorts)
        {
            if (sorts == null)
            {
                throw new ArgumentNullException(nameof(sorts), "Sort collection cannot be null.");
            }

            if (sorts.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>(sorts.Count);
            for (int i = 0; i < sorts.Count; i++)
            {
                var item = sorts[i];
                var dir = (item.Direction == SortDirection.Desc) ? "DESC" : "ASC";
                parts.Add(item.Field + " " + dir);
            }

            return "ORDER BY " + string.Join(", ", parts);
        }
    }
}
