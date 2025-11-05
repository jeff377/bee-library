using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 負責依據查詢需求產生完整 SelectContext 內容的建構類別。
    /// 依據指定的查詢欄位、條件、排序等，建立對應的 QueryFieldMapping 與 TableJoin 集合。
    /// </summary>
    public class SelectContextBuilder
    {
        private FormTable _formTable;
        private string _currentTableAlias = "A";  // 目前使用的資料表別名

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        public SelectContextBuilder(FormTable formTable)
        {
            _formTable = formTable;
        }

        /// <summary>
        /// 產生 SelectContext。
        /// </summary>
        public SelectContext Build()
        {
            var context = new SelectContext();

            // 主表的資料表別名為 A
            _currentTableAlias = "A";

            foreach (var field in _formTable.Fields)
            {
                // 針對外鍵欄位，建立資料表之間的 Join 關係
                if (field.Type == FieldType.DbField && StrFunc.IsNotEmpty(field.RelationProgId))
                {
                    // 以「主表名.欄位名.SourceProgId」當 Join 唯一鍵
                    string key = $"{_formTable.TableName}.{field.FieldName}.{field.RelationProgId}";
                    AddTableJoin(context, key, field, _formTable.DbTableName, _currentTableAlias);
                }
            }
            return context;
        }

        /// <summary>
        /// 依據外鍵欄位，將兩個資料表之間的 Join 關係加入至 SelectContext。
        /// </summary>
        /// <param name="context">SelectContext 實例。</param>
        /// <param name="key">Join 關係的唯一鍵值。</param>
        /// <param name="foreignKeyField">外鍵欄位。</param>
        /// <param name="leftTable">左側資料表名稱。</param>
        /// <param name="leftAlias">左側資料表別名。</param>
        /// <param name="fieldMappings">指定欄位對應集合，遞迴處理多層關連時需指定。</param>
        /// <param name="queryFieldName">指定建立 QueryFieldMapping 的欄位名稱，遞迴處理多層關連時需指定。</param>
        private void AddTableJoin(SelectContext context, string key, FormField foreignKeyField, string leftTable, string leftAlias,
            FieldMappingCollection fieldMappings = null, string queryFieldName = "")
        {
            var srcFormDefine = BackendInfo.DefineAccess.GetFormDefine(foreignKeyField.RelationProgId);
            var srcTable = srcFormDefine.MasterTable;

            // 若尚未存在對應的 Join，就建立
            var join = context.Joins.GetOrDefault(key);
            if (join == null)
            {
                join = new TableJoin()
                {
                    Key = key,
                    LeftTable = leftTable,
                    LeftAlias = leftAlias,
                    LeftField = foreignKeyField.FieldName,
                    RightTable = srcTable.DbTableName,
                    RightAlias = GetActiveTableAlias(),
                    RightField = SysFields.RowId
                };
                context.Joins.Add(join);
            }

            var mappings = fieldMappings ?? foreignKeyField.RelationFieldMappings;

            foreach (var mapping in mappings)
            {
                var srcField = srcTable.Fields.GetOrDefault(mapping.SourceField);
                if (srcField == null)
                {
                    throw new InvalidOperationException(
                        $"Source field '{mapping.SourceField}' not found in table '{srcTable.TableName}' " +
                        $"for foreign key field '{foreignKeyField.FieldName}' in relation '{foreignKeyField.RelationProgId}'.");
                }

                // 遞迴處理多層 RelationField（若來源欄位仍是關聯）
                if (srcField.Type == FieldType.RelationField)
                {
                    var reference = srcTable.RelationFieldReferences[srcField.FieldName];
                    string srcKey = key + "." + reference.ForeignKeyField.RelationProgId;
                    var srcMappings = CreateSingleFieldMappings(reference.ForeignKeyField.RelationFieldMappings, reference.FieldName);
                    AddTableJoin(context, srcKey, reference.ForeignKeyField, join.RightTable, join.RightAlias, srcMappings, mapping.DestinationField);
                }
                else
                {
                    var fieldMapping = new QueryFieldMapping()
                    {
                        FieldName = StrFunc.IsEmpty(queryFieldName) ? mapping.DestinationField : queryFieldName,
                        SourceAlias = join.RightAlias,
                        SourceField = srcField.FieldName,
                        TableJoin = join
                    };
                    context.FieldMappings.Add(fieldMapping);
                }
            }
        }

        /// <summary>
        /// 建立只包含單一欄位對應的集合。
        /// </summary>
        /// <param name="sourceMappings">來源對應集合。</param>
        /// <param name="destinationField">目標欄位名稱。</param>
        private FieldMappingCollection CreateSingleFieldMappings(FieldMappingCollection sourceMappings, string destinationField)
        {
            var fieldMapping = sourceMappings.FindByDestination(destinationField);
            var mappings = new FieldMappingCollection();
            mappings.Add(fieldMapping.SourceField, fieldMapping.DestinationField);
            return mappings;
        }

        /// <summary>
        /// 取得下一個資料表別名。
        /// </summary>
        /// <param name="tableAlias">目前資料表別名。</param>
        public static string GetNextTableAlias(string tableAlias)
        {
            string baseValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string nextAlias = StrFunc.GetNextId(tableAlias, baseValues);
            // 若資料表別名為關鍵字，則重取資料表別名
            if (StrFunc.IsEqualsOr(nextAlias, "AS", "BY", "IF", "IN", "IS", "OF", "OR", "TO", "ON"))
                nextAlias = StrFunc.GetNextId(tableAlias, baseValues);
            return nextAlias;
        }

        /// <summary>
        /// 取得作用的資料表別名。
        /// </summary>
        private string GetActiveTableAlias()
        {
            _currentTableAlias = GetNextTableAlias(_currentTableAlias);
            return _currentTableAlias;
        }
    }
}
