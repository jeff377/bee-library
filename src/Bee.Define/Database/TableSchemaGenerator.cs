using Bee.Define.Forms;
using Bee.Base;
using System;

namespace Bee.Define.Database
{
    /// <summary>
    /// 資料表結構生成器。
    /// 負責將 FormTable 轉換為 TableSchema 結構。
    /// </summary>
    public class TableSchemaGenerator
    {
        /// <summary>
        /// 生成資料表結構。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <returns>資料表結構。</returns>
        public TableSchema Generate(FormTable formTable)
        {
            if (formTable == null)
                throw new ArgumentNullException(nameof(formTable));

            var tableSchema = new TableSchema
            {
                TableName = StrFunc.IsEmpty(formTable.DbTableName) ? formTable.TableName : formTable.DbTableName,
                DisplayName = formTable.DisplayName
            };

            AddFields(formTable, tableSchema);
            AddIndexes(formTable, tableSchema);

            return tableSchema;
        }

        /// <summary>
        /// 加入欄位。
        /// </summary>
        private void AddFields(FormTable formTable, TableSchema tableSchema)
        {
            if (formTable.Fields == null) return;

            foreach (var field in formTable.Fields)
            {
                // 只處理資料庫相關欄位
                if (field.Type != FieldType.DbField)
                    continue;

                var dbField = new DbField(field.FieldName, field.Caption, field.DbType)
                {
                    Length = field.MaxLength,
                    DefaultValue = field.DefaultValue
                };

                tableSchema.Fields.Add(dbField);
            }
        }

        /// <summary>
        /// 加入索引。
        /// </summary>
        private void AddIndexes(FormTable formTable, TableSchema tableSchema)
        {
            // 前置檢查，避免空值判斷
            if (tableSchema.Fields == null) return;

            // 建立主索引
            if (tableSchema.Fields.Contains(SysFields.No))
                tableSchema.Indexes.AddPrimaryKey(SysFields.No);

            // 建立唯一列別索引
            if (tableSchema.Fields.Contains(SysFields.RowId))
                tableSchema.Indexes.Add("rx_{0}", SysFields.RowId, true);

            // 建立唯一流水索引
            if (tableSchema.Fields.Contains(SysFields.Id))
                tableSchema.Indexes.Add("uk_{0}", SysFields.Id, true);

            // 建立外鍵索引
            if (formTable.Fields == null) { return; }
            foreach (var field in formTable.Fields)
            {
                if (StrFunc.IsNotEmpty(field.RelationProgId))
                {
                    // 包含欄位名稱以避免重複
                    tableSchema.Indexes.Add("fk_{0}_{field.FieldName}", field.FieldName, false);
                }
            }
        }
    }
}
