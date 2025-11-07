using System;
using System.Collections.Generic;
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
        private HashSet<string> _usedFieldNames;
        private string _currentTableAlias = "A";  // 目前使用的資料表別名

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="usedFieldNames">查詢使用到的欄位名稱集合。</param>
        public SelectContextBuilder(FormTable formTable, HashSet<string> usedFieldNames)
        {
            _formTable = formTable;
            _usedFieldNames = usedFieldNames;
        }

        /// <summary>
        /// 產生 SelectContext。
        /// </summary>
        public SelectContext Build()
        {
            var context = new SelectContext();

            // 主表的資料表別名為 A
            _currentTableAlias = "A";

            // 針對外鍵欄位，建立資料表之間的 Join 關係
            foreach (var field in _formTable.Fields)
            {
                // 非外鍵欄位則略濄
                if (field.Type != FieldType.DbField || StrFunc.IsEmpty(field.RelationProgId)) { continue; }

                // 由外鍵欄位關連取回的參照欄位對應集合
                var fieldMappings = GetUsedRelationFieldMappings(field);
                if (BaseFunc.IsEmpty(fieldMappings)) { continue; }

                // 以「主表名.欄位名.SourceProgId」當 Join 唯一鍵
                string key = $"{_formTable.TableName}.{field.FieldName}.{field.RelationProgId}";
                AddTableJoin(context, key, field, fieldMappings, _formTable.DbTableName, _currentTableAlias);
            }
            return context;
        }

        /// <summary>
        /// 依據外鍵欄位，將兩個資料表之間的 Join 關係加入至 SelectContext。
        /// </summary>
        /// <param name="context">SelectContext 實例。</param>
        /// <param name="key">Join 關係的唯一鍵值。</param>
        /// <param name="foreignKeyField">外鍵欄位。</param>
        /// <param name="fieldMappings">由外鍵欄位關連取回的參照欄位對應集合。</param>
        /// <param name="leftTable">左側資料表名稱。</param>
        /// <param name="leftAlias">左側資料表別名。</param>
        /// <param name="queryFieldName">指定建立 QueryFieldMapping 的欄位名稱，遞迴處理多層關連時需指定。</param>
        private void AddTableJoin(SelectContext context, string key, FormField foreignKeyField, FieldMappingCollection fieldMappings,
            string leftTable, string leftAlias, string queryFieldName = "")
        {
            var srcFormDefine = BackendInfo.DefineAccess.GetFormDefine(foreignKeyField.RelationProgId);
            if (srcFormDefine == null)
            {
                throw new InvalidOperationException(
                    $"Form definition '{foreignKeyField.RelationProgId}' not found for field '{foreignKeyField.FieldName}'.");
            }
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

            foreach (var mapping in fieldMappings)
            {
                var srcField = srcTable.Fields.GetOrDefault(mapping.SourceField);
                if (srcField == null)
                {
                    throw new InvalidOperationException(
                        $"Source field '{mapping.SourceField}' not found in table '{srcTable.TableName}' " +
                        $"for foreign key field '{foreignKeyField.FieldName}' in relation '{foreignKeyField.RelationProgId}'.");
                }

                if (srcField.Type == FieldType.RelationField)
                {
                    // 若來源欄位仍是關聯，使用遞迴處理巢狀關聯
                    var reference = srcTable.RelationFieldReferences[srcField.FieldName];
                    string srcKey = key + "." + reference.ForeignKeyField.RelationProgId;
                    var srcMappings = GetSingleRelationFieldMappings(reference.ForeignKeyField, reference.FieldName);
                    AddTableJoin(context, srcKey, reference.ForeignKeyField, srcMappings, join.RightTable, join.RightAlias, mapping.DestinationField);
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
        /// 關鍵字關鍵字集合。
        /// </summary>
        private static readonly HashSet<string> SqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AS", "BY", "IF", "IN", "IS", "OF", "OR", "TO", "ON",
            "GO", "NO", "DO", "AT", "IT"
        };

        /// <summary>
        /// 取得下一個資料表別名。
        /// </summary>
        /// <param name="tableAlias">目前資料表別名。</param>
        private static string GetNextTableAlias(string tableAlias)
        {
            // 採用 26 進位，A → B → C ... → Z → ZA → ZB ... （多位數展開）
            string baseValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string nextAlias = StrFunc.GetNextId(tableAlias, baseValues);
            // 若資料表別名為關鍵字，則重取資料表別名
            while (SqlKeywords.Contains(nextAlias))
            {
                nextAlias = StrFunc.GetNextId(nextAlias, baseValues);
            }
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

        /// <summary>
        /// 針對指定外鍵欄位，判斷 _usedFieldNames 中哪些欄位為其關連取回欄位，並回傳新的欄位對應集合。
        /// </summary>
        /// <param name="foreignKeyField">外鍵欄位。</param>
        /// <returns>符合條件的欄位對應集合。</returns>
        private FieldMappingCollection GetUsedRelationFieldMappings(FormField foreignKeyField)
        {
            var result = new FieldMappingCollection();
            if (foreignKeyField.RelationFieldMappings == null)
                return result;

            foreach (var mapping in foreignKeyField.RelationFieldMappings)
            {
                // 判斷 _usedFieldNames 是否包含此關連欄位
                if (_usedFieldNames.Contains(mapping.DestinationField))
                {
                    result.Add(mapping.SourceField, mapping.DestinationField);
                }
            }
            return result;
        }

        /// <summary>
        /// 取得指定外鍵欄位的單一目的欄位對應集合。
        /// </summary>
        /// <param name="foreignKeyField">外鍵欄位。</param>
        /// <param name="destinationField">目標欄位名稱。</param>
        private FieldMappingCollection GetSingleRelationFieldMappings(FormField foreignKeyField, string destinationField)
        {
            var fieldMapping = foreignKeyField.RelationFieldMappings.FindByDestination(destinationField);
            var result = new FieldMappingCollection();
            result.Add(fieldMapping.SourceField, fieldMapping.DestinationField);
            return result;
        }
    }
}
