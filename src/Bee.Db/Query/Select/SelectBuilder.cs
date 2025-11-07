using Bee.Base;
using Bee.Define;
using System;
using System.Collections.Generic;

namespace Bee.Db
{
    /// <summary>
    /// SELECT 子句建置器。
    /// </summary>
    public class SelectBuilder : ISelectBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        public SelectBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// 建立 SELECT 子句。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFieldNames">要選取的欄位名稱集合。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        public string Build(FormTable formTable, StringHashSet selectFieldNames, SelectContext selectContext)
        {
            var selectParts = new List<string>();
            foreach (var fieldName in selectFieldNames)
            {
                var field = formTable.Fields.GetOrDefault(fieldName);
                if (field == null)
                    throw new InvalidOperationException($"Field '{fieldName}' does not exist in table '{formTable.TableName}'.");
                if (field.Type == FieldType.DbField)
                {
                    selectParts.Add($"    A.{QuoteIdentifier(fieldName)}");
                }
                else
                {
                    var mapping = selectContext.FieldMappings.GetOrDefault(fieldName);
                    if (mapping == null)
                        throw new InvalidOperationException($"Field mapping for '{fieldName}' is null.");
                    selectParts.Add($"    {mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)} AS {QuoteIdentifier(fieldName)}");
                }
            }
            return "SELECT\n" + string.Join(",\n", selectParts);
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
