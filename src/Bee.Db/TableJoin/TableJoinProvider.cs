using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料表關連資訊提供者，產生 JOIN 語法時使用。
    /// </summary>
    internal class TableJoinProvider
    {
        private readonly TableJoinCollection _TableJoins = null;
        private readonly LinkFieldMappingCollection _Mappings = null;
        private readonly IDbCommandHelper _Helper = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="helper">資料庫命令輔助類別。</param>
        public TableJoinProvider(IDbCommandHelper helper)
        {
            _TableJoins = new TableJoinCollection();
            _Mappings = new LinkFieldMappingCollection();
            _Helper = helper;
        }

        #endregion

        /// <summary>
        /// 資料表關連集合。
        /// </summary>
        public TableJoinCollection TableJoins
        {
            get { return _TableJoins; }
        }

        /// <summary>
        /// 關連欄位對應集合。
        /// </summary>
        public LinkFieldMappingCollection Mappings
        {
            get { return _Mappings; }
        }

        /// <summary>
        /// 資料庫命令輔助類別。
        /// </summary>
        private IDbCommandHelper Helper
        {
            get { return _Helper; }
        }

        /// <summary>
        /// 取得 SQL 語法中的原始欄位，包含資料表別名的欄位名稱。
        /// </summary>
        /// <param name="field">表單欄位。</param>
        public string GetFIeldName(FormField field)
        {
            LinkFieldMapping oMapping;

            oMapping = this.Mappings[field.FieldName];
            if (oMapping == null)
                return $"A.{this.Helper.QuoteIdentifier(field.FieldName)}";
            else
                return $"{oMapping.SourceTableAlias}.{this.Helper.QuoteIdentifier(oMapping.SourceField)}";
        }
    }
}
