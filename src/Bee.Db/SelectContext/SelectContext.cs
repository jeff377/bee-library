namespace Bee.Db
{
    /// <summary>
    /// 描述一次 Select 查詢時所需的欄位來源與 Join 關係集合。
    /// 包含查詢中所有使用到的欄位對應（Select、Where 等）及相關的 TableJoin 設定，
    /// 以便組合完整的 SQL 查詢語法。
    /// </summary>
    public class SelectContext
    {
        /// <summary>
        /// 查詢中所有使用到的欄位來源對應集合。
        /// 每個項目描述查詢欄位與其原始資料來源的關係。
        /// </summary>
        public QueryFieldMappingCollection FieldMappings { get; set; } = new QueryFieldMappingCollection();

        /// <summary>
        /// 查詢中所需的所有 TableJoin 關係集合。
        /// 用於描述資料表之間的 Join 條件與結構。
        /// </summary>
        public TableJoinCollection Joins { get; set; } = new TableJoinCollection();
    }
}
