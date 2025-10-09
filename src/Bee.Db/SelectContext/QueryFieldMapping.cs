using System;
using System.Collections.Generic;
using System.Text;
using Bee.Base;

namespace Bee.Db
{
    /// <summary>
    /// 描述查詢中欄位與其原始資料來源的對應關係。
    /// 查詢欄位包含 Select 或 Where 子句使用到的欄位。
    /// </summary>
    public class QueryFieldMapping : KeyCollectionItem
    {
        /// <summary>
        /// 查詢中使用的欄位名稱。
        /// </summary>
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 原始資料表別名。
        /// </summary>
        public string SourceAlias { get; set; }

        /// <summary>
        /// 原始資料表欄位名稱。
        /// </summary>
        public string SourceField { get; set; }
    }
}
