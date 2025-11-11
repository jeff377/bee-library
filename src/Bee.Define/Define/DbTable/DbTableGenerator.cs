using Bee.Base;
using System;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫資料表生成器。
    /// 負責將 FormTable 轉換為 DbTable 結構。
    /// </summary>
    public class DbTableGenerator
    {
        /// <summary>
        /// 生成資料庫資料表。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <returns>資料庫資料表。</returns>
        public DbTable Generate(FormTable formTable)
        {
            if (formTable == null)
                throw new ArgumentNullException(nameof(formTable));

            var dbTable = new DbTable
            {
                TableName = StrFunc.IsEmpty(formTable.DbTableName) ? formTable.TableName : formTable.DbTableName,
                DisplayName = formTable.DisplayName
            };

            AddFields(formTable, dbTable);
            AddIndexes(formTable, dbTable);

            return dbTable;
        }

        /// <summary>
        /// 加入欄位。
        /// </summary>
        private void AddFields(FormTable formTable, DbTable dbTable)
        {
            if (formTable.Fields == null) return;

            foreach (var field in formTable.Fields)
            {
                // 只處理資料庫欄位類型
                if (field.Type != FieldType.DbField)
                    continue;

                var dbField = new DbField(field.FieldName, field.Caption, field.DbType)
                {
                    Length = field.MaxLength,
                    DefaultValue = field.DefaultValue
                };

                dbTable.Fields.Add(dbField);
            }
        }

        /// <summary>
        /// 加入索引。
        /// </summary>
        private void AddIndexes(FormTable formTable, DbTable dbTable)
        {
            // 提前檢查,避免重複判斷
            if (dbTable.Fields == null) return;

            // 建立主鍵索引
            if (dbTable.Fields.Contains(SysFields.No))
                dbTable.Indexes.AddPrimaryKey(SysFields.No);

            // 建立唯一識別索引
            if (dbTable.Fields.Contains(SysFields.RowId))
                dbTable.Indexes.Add("rx_{0}", SysFields.RowId, true);

            // 建立唯一資料索引
            if (dbTable.Fields.Contains(SysFields.Id))
                dbTable.Indexes.Add("uk_{0}", SysFields.Id, true);

            // 建立外鍵索引
            if (formTable.Fields == null) { return; }
            foreach (var field in formTable.Fields)
            {
                if (StrFunc.IsNotEmpty(field.RelationProgId))
                {
                    // 包含欄位名稱以避免衝突
                    dbTable.Indexes.Add("fk_{0}_{field.FieldName}", field.FieldName, false);
                }
            }

        }

    }
}