using System.Collections.Generic;

namespace Bee.Db
{
    /// <summary>
    /// WHERE 組合結果。
    /// </summary>
    public sealed class WhereBuildResult
    {
        /// <summary>WHERE 子句字串（可含或不含 "WHERE" 關鍵字）。</summary>
        public string WhereClause { get; set; }

        /// <summary>具名參數。</summary>
        public IReadOnlyDictionary<string, object> Parameters { get; set; }
    }
}
