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
                    if (_formTable.DestinationFieldMap.ContainsKey(fieldName))
                    {                       
                        var foreignKeyField = _formTable.DestinationFieldMap[fieldName];
                        string tableAlias = "A";
                        string relationProgId = _formTable.DestinationFieldMap[fieldName].RelationProgId;
                        string key = $"{_formTable.TableName}.{field.FieldName}.{relationProgId}";
                        AddTableJoin(context, key, _formTable.DbTableName, tableAlias, fieldName, foreignKeyField);
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
        /// <param name="fieldName">要取回的欄位名稱。</param>
        /// <param name="foreignKeyField">對應外部表的主鍵欄位。</param>
        private void AddTableJoin(SelectContext context, string key, string tableName, string tableAlias, string fieldName, FormField foreignKeyField)
        {
            var formDefine = CacheFunc.GetFormDefine(foreignKeyField.RelationProgId);
            var formTable = formDefine.MasterTable;

            foreach (var mapping in foreignKeyField.RelationFieldMappings)
            {
                var sourceField = formTable.Fields[mapping.SourceField];
                var destField = _formTable.Fields[mapping.DestinationField];

                // 檢查是否已存在對應的欄位對應
                if (!context.FieldMappings.Contains(key))
                {
                    // 加入欄位對應 (假設 sourceField.FieldName 為來源欄位, key 為目的欄位)
                    var join = new TableJoin()
                    {
                        Key = key,
                        LeftTable = tableName,
                        LeftAlias = tableAlias,
                        RightTable = formTable.DbTableName,
                        RightAlias = GetNextTableAlias(tableAlias)
                    };
                    string leftFIeld = $"{join.LeftAlias},{foreignKeyField.FieldName}";
                    string rightField = $"{join.RightAlias},{SysFields.RowId}";
                    join.Conditions.Add(new JoinCondition(leftFIeld, rightField));
                }
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
