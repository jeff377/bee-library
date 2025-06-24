using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 關連欄位對應。
    /// </summary>
    internal class LinkFieldMapping : KeyCollectionItem
    {
        private string _SourceTableAlias = string.Empty;
        private string _SourceField = string.Empty;

        /// <summary>
        /// 目的欄位。
        /// </summary>
        public string DestinationField
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 來源資料表別名。
        /// </summary>
        public string SourceTableAlias
        {
            get { return _SourceTableAlias; }
            set { _SourceTableAlias = value; }
        }

        /// <summary>
        /// 來源欄位。
        /// </summary>
        public string SourceField
        {
            get { return _SourceField; }
            set { _SourceField = value; }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.SourceTableAlias}.{this.SourceField} As {this.DestinationField}";
        }
    }
}
