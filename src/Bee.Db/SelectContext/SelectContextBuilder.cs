using Bee.Base;
using Bee.Cache;
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

            // 主表固定 A
            string mainAlias = "A";

            foreach (var field in _formTable.Fields)
            {
                if (field.Type != FieldType.RelationField) continue;

                var fieldName = field.FieldName;
                if (!_formTable.RelationFieldReferences.Contains(fieldName)) continue;

                var reference = _formTable.RelationFieldReferences[fieldName];
                // 以「主表名.欄位名.SourceProgId」當 Join 唯一鍵
                string key = $"{_formTable.TableName}.{fieldName}.{reference.SourceProgId}";
                AddTableJoin(context, key, _formTable.DbTableName, mainAlias, reference);
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

            // 若尚未存在對應的 Join，就建立
            var join = context.Joins.GetOrDefault(key);
            if (join == null)
            {
                join = new TableJoin()
                {
                    Key = key,
                    LeftTable = tableName,
                    LeftAlias = tableAlias,
                    LeftField = reference.FieldName,
                    RightTable = srcTable.DbTableName,
                    RightAlias = GetNextTableAlias(tableAlias),
                    RightField = SysFields.RowId
                };
                context.Joins.Add(join);
            }

            // 遞迴處理多層 RelationField（若來源欄位仍是關聯）
            if (srcField.Type == FieldType.RelationField)
            {
                var srcReference = srcTable.RelationFieldReferences[srcField.FieldName];
                string srcKey = key + "." + srcReference.SourceProgId;
                AddTableJoin(context, srcKey, join.RightTable, join.RightAlias, srcReference);
            }
            else
            {
                var fieldMapping = new QueryFieldMapping()
                {
                    FieldName = reference.FieldName,
                    SourceAlias = join.RightAlias,
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
            if (StrFunc.IsEqualsOr(nextAlias, "AS", "BY", "IF", "IN", "IS", "OF", "OR", "TO", "ON"))
                nextAlias = StrFunc.GetNextId(tableAlias, baseValues);
            return nextAlias;
        }


    }
}
