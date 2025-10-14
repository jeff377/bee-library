using System.Collections.Generic;
using Bee.Base;
using Bee.Cache;
using Bee.Define;
using MessagePack.Resolvers;

namespace Bee.Db
{
    /// <summary>
    /// 負責依據查詢需求產生完整 SelectContext 內容的建構類別。
    /// 依據指定的查詢欄位、條件、排序等，建立對應的 QueryFieldMapping 與 TableJoin 集合。
    /// </summary>
    public class SelectContextBuilder
    {
        private FormTable _formTable;

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
        /// <param name="fieldNames">欄位清單（包含 Select、Where 及 Order 子句欄位）。</param>
        public SelectContext Build(IEnumerable<string> fieldNames)
        {
            var context = new SelectContext();

            foreach (string fieldName in fieldNames)
            {
                var field = _formTable.Fields[fieldName];
                if (field != null && field.Type == FieldType.RelationField)
                {
                    if (_formTable.RelationFieldReferences.Contains(fieldName))
                    {
                        var reference = _formTable.RelationFieldReferences[fieldName];
                        string tableAlias = "A";
                        string key = $"{_formTable.TableName}.{fieldName}.{reference.SourceProgId}";
                        AddTableJoin(context, key, _formTable.DbTableName, tableAlias, reference);
                    }
                }
            }
            return context;
        }

        /// <summary>
        /// 加入兩個資料表之間的 Join 關係到 SelectContext。
        /// 若尚未存在對應的 Join，則建立並加入 Joins 集合。
        /// </summary>
        /// <param name="context">SelectContext 實例。</param>
        /// <param name="key">Join 關係的唯一鍵值。</param>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="tableAlias">資料表別名。</param>
        /// <param name="reference">關連欄位的參照來源。</param>
        private void AddTableJoin(SelectContext context, string key, string tableName, string tableAlias, RelationFieldReference reference)
        {
            var srcFormDefine = CacheFunc.GetFormDefine(reference.SourceProgId);
            var srcTable = srcFormDefine.MasterTable;

            var srcField = srcTable.Fields[reference.SourceField];
            var destField = _formTable.Fields[reference.FieldName];

            // 檢查是否已存在對應的欄位對應
            var tableJoin = context.Joins[key];
            if (tableJoin == null)
            {
                tableJoin = new TableJoin()
                {
                    Key = key,
                    LeftTable = tableName,
                    LeftAlias = tableAlias,
                    RightTable = srcTable.DbTableName,
                    RightAlias = GetNextTableAlias(tableAlias)
                };
                string leftFIeld = $"{tableJoin.LeftAlias},{reference.FieldName}";
                string rightField = $"{tableJoin.RightAlias},{SysFields.RowId}";
                tableJoin.Conditions.Add(new JoinCondition(leftFIeld, rightField));
            }

            if (srcField.Type == FieldType.RelationField)
            {
                // 若來源欄位是 RelationField，則需往上階找原始關連來源
                var srcReference = srcTable.RelationFieldReferences[srcField.FieldName];
                string srcKey = key + "." + srcReference.SourceProgId;
                AddTableJoin(context, srcKey, tableJoin.RightTable, tableJoin.RightAlias, srcReference);
            }
            else
            {
                var fieldMapping = new QueryFieldMapping()
                {
                    FieldName = reference.FieldName,
                    SourceAlias = tableJoin.RightAlias,
                    SourceField = reference.SourceField
                };
                context.FieldMappings.Add(fieldMapping);
            }
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
            if (StrFunc.IsEqualsOr(nextAlias, "AS", "BY", "IF", "IN", "IS", "OF", "OR", "TO"))
                nextAlias = StrFunc.GetNextId(tableAlias, baseValues);
            return nextAlias;
        }


    }
}
