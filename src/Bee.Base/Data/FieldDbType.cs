namespace Bee.Base.Data
{
    /// <summary>
    /// 欄位資料型別。
    /// 抽象資料欄位型別（跨資料庫對應）。
    /// </summary>
    public enum FieldDbType
    {
        /// <summary>
        /// 字串。
        /// </summary>
        String,
        /// <summary>
        /// 長文字。
        /// </summary>
        Text,
        /// <summary>
        /// 布林。
        /// </summary>
        Boolean,
        /// <summary>
        /// 自動遞增整數。
        /// </summary>
        AutoIncrement,
        /// <summary>
        /// 16 位元整數 (-32,768 到 32,767)。
        /// </summary>
        Short,
        /// <summary>
        /// 32 位元整數 (-2,147,483,648 到 2,147,483,647)。
        /// </summary>
        Integer,
        /// <summary>
        /// 64 位元整數 (Long)。
        /// </summary>
        Long,
        /// <summary>
        /// 十進位數值（高精度）。
        /// </summary>
        Decimal,
        /// <summary>
        /// 貨幣。
        /// </summary>
        Currency,
        /// <summary>
        /// 日期。
        /// </summary>
        Date,
        /// <summary>
        /// 日期時間。
        /// </summary>
        DateTime,
        /// <summary>
        /// Guid 值。
        /// </summary>
        Guid,
        /// <summary>
        /// 二進位資料。
        /// </summary>
        Binary,
        /// <summary>
        /// 未知。
        /// </summary>
        Unknown
    }
}
