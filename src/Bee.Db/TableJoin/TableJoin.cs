using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料表關連。
    /// </summary>
    internal class TableJoin : KeyCollectionItem
    {
        private string _LeftTableAlias = string.Empty;
        private string _LeftField = string.Empty;
        private string _RightTable = string.Empty;
        private string _RightTableAlias = string.Empty;
        private string _RightField = string.Empty;

        /// <summary>
        /// 關連鍵值，關連鍵值格式為 "欄位$關連程式代碼階層"。
        /// </summary>
        /// <remarks>
        /// 關連程式代碼會有多階情形，例如員工編號欄位透過「員工->部門」取得部門名稱，鍵值為「員工編號欄位$員工程式代碼.部門程式代碼」。
        /// </remarks>
        public override string Key
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 左側資料表別名。
        /// </summary>
        public string LeftTableAlias
        {
            get { return _LeftTableAlias; }
            set { _LeftTableAlias = value; }
        }

        /// <summary>
        /// 左側欄位。
        /// </summary>
        public string LeftField
        {
            get { return _LeftField; }
            set { _LeftField = value; }
        }

        /// <summary>
        /// 右側資料表。
        /// </summary>
        public string RightTable
        {
            get { return _RightTable; }
            set { _RightTable = value; }
        }

        /// <summary>
        /// 右側資料表別名。
        /// </summary>
        public string RightTableAlias
        {
            get { return _RightTableAlias; }
            set { _RightTableAlias = value; }
        }

        /// <summary>
        /// 右側欄位。
        /// </summary>
        public string RightField
        {
            get { return _RightField; }
            set { _RightField = value; }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"Left Join {this.RightTable} {this.RightTableAlias} On {this.LeftTableAlias}.{this.LeftField}={this.RightTableAlias}.{this.RightField}";
        }
    }
}
