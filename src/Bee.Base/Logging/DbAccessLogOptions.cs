using System;
using System.ComponentModel;

namespace Bee.Base
{
    /// <summary>
    /// DbAccess 模組的記錄選項。
    /// </summary>
    [Serializable]
    [Description("DbAccess 模組的記錄選項。")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DbAccessLogOptions
    {
        /// <summary>
        /// 記錄層級（Error：僅錯誤、Warning：包含異常、All：所有 SQL）。
        /// </summary>
        [Description("記錄層級（Error：僅錯誤、Warning：包含異常、All：所有 SQL）。")]
        public DbAccessLogLevel Level { get; set; } = DbAccessLogLevel.Warning;

        /// <summary>
        /// SQL 操作所影響的資料筆數異常門檻，超過此值視為異常（預設 10000）。
        /// </summary>
        [Description("SQL 操作所影響的資料筆數異常門檻，超過此值視為異常（預設 10000）。")] 
        public int AffectedRowThreshold { get; set; } = 10000;

        /// <summary>
        /// 查詢執行時間異常門檻（單位：秒），超過此秒數視為慢查詢。
        /// </summary>
        [Description("查詢執行時間異常門檻（單位：秒），超過此秒數視為慢查詢。")]
        public int SlowQueryThreshold { get; set; } = 300;

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
