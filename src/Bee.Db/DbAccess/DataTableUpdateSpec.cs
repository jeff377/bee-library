using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// 承載 DataTable 更新所需的資料表與三個命令描述。
    /// </summary>
    public sealed class DataTableUpdateSpec
    {
        /// <summary>
        /// 要寫入資料庫的資料表。
        /// </summary>
        public DataTable DataTable { get; set; }

        /// <summary>
        /// 新增命令描述。
        /// </summary>
        public DbCommandSpec InsertCommand { get; set; }

        /// <summary>
        /// 更新命令描述。
        /// </summary>
        public DbCommandSpec UpdateCommand { get; set; }

        /// <summary>
        /// 刪除命令描述。
        /// </summary>
        public DbCommandSpec DeleteCommand { get; set; }

        /// <summary>
        /// 是否使用交易包覆整個異動（任何一筆失敗就回滾；成功則提交）。
        /// </summary>
        public bool UseTransaction { get; set; } = false;

        /// <summary>
        /// 交易隔離等級（當 <see cref="UseTransaction"/> 為 true 時可指定）。
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }
    }
}
