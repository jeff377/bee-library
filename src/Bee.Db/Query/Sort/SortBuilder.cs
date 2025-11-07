using System;
using System.Collections.Generic;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// ORDER BY 子句建置器。
    /// </summary>
    public sealed class SortBuilder : ISortBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        public SortBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// 根據指定的排序欄位集合，產生 SQL 的 ORDER BY 子句（包含前綴關鍵字）。
        /// </summary>
        /// <param name="sortFields">排序欄位集合。</param>
        /// <param name="selectContext">表示 SQL 查詢所需的欄位來源與資料表 Join 關係集合。</param>
        public string Build(SortFieldCollection sortFields, SelectContext selectContext = null)
        {
            if (BaseFunc.IsEmpty(sortFields)) { return string.Empty; }

            var mappedSortFields = (selectContext != null)
                   ? RemapSortFields(sortFields, selectContext)
                   : sortFields;

            var parts = new List<string>(mappedSortFields.Count);
            for (int i = 0; i < mappedSortFields.Count; i++)
            {
                var item = mappedSortFields[i];
                var dir = (item.Direction == SortDirection.Desc) ? "DESC" : "ASC";
                parts.Add(item.FieldName + " " + dir);
            }

            return "ORDER BY " + string.Join(", ", parts);
        }

        /// <summary>
        /// 依據查詢欄位來源，產生 SortFIeldCollection 的複本並加上正確的 SQL 欄位表達式。
        /// </summary>
        /// <param name="sortFields">原始排序欄位集合。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        private SortFieldCollection RemapSortFields(SortFieldCollection sortFields, SelectContext selectContext)
        {
            var result = new SortFieldCollection();
            foreach (var sortField in sortFields)
            {
                var mapping = selectContext.FieldMappings.GetOrDefault(sortField.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                }
                else
                {
                    fieldExpr = $"A.{QuoteIdentifier(sortField.FieldName)}";
                }
                result.Add(new SortField(fieldExpr, sortField.Direction));
            }
            return result;
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
