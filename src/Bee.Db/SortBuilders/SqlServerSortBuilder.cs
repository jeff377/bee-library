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
        /// 根據指定的排序欄位集合，產生 SQL 的 ORDER BY 子句（包含前綴關鍵字）。
        /// </summary>
        /// <param name="sortFields">排序欄位集合。</param>
        public string Build(SortFIeldCollection sortFields)
        {
            if (sortFields == null)
                throw new ArgumentNullException(nameof(sortFields), "Sort field collection cannot be null.");

            if (sortFields.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>(sortFields.Count);
            for (int i = 0; i < sortFields.Count; i++)
            {
                var item = sortFields[i];
                var dir = (item.Direction == SortDirection.Desc) ? "DESC" : "ASC";
                parts.Add(item.FieldName + " " + dir);
            }

            return "ORDER BY " + string.Join(", ", parts);
        }
    }
}
