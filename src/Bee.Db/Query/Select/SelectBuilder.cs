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
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        public string Build(FormTable formTable, string selectFields, SelectContext selectContext)
        {
            var selectFieldNames = GetSelectFields(formTable, selectFields);
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

        /// <summary>
        /// 取得 Select 的欄位集合。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位</param>
        private StringHashSet GetSelectFields(FormTable formTable, string selectFields)
        {
            var set = new StringHashSet();
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                // 取得所有欄位
                foreach (var field in formTable.Fields)
                {
                    set.Add(field.FieldName);
                }
            }
            else
            {
                // 只取指定欄位
                set.Add(selectFields, ",");
            }
            return set;
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
