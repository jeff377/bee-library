namespace Bee.Db
{
    /// <summary>
    /// 表示 SQL 查詢所需的欄位來源與資料表 Join 關係集合。
    /// 此類別用於記錄查詢過程中所有涉及的欄位對應（如 Select、Where、Order 子句）及相關的 TableJoin 設定，
    /// 以便組合出完整且正確的 SQL 查詢語法。
    /// </summary>
    public class SelectContext
    {
        /// <summary>
        /// 查詢所使用的所有欄位來源對應集合。
        /// 每個項目描述查詢欄位與其原始資料表、欄位及 Join 關係的對應資訊。
        /// </summary>
        public QueryFieldMappingCollection FieldMappings { get; set; } = new QueryFieldMappingCollection();

        /// <summary>
        /// 查詢所需的所有資料表 Join 關係集合。
        /// 用於記錄資料表之間的 Join 條件、結構及別名等資訊。
        /// </summary>
        public TableJoinCollection Joins { get; set; } = new TableJoinCollection();
    }
}
