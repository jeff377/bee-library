using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// 執行批次命令的描述。
    /// </summary>
    public class DbBatchSpec
    {
        /// <summary>
        /// 要執行的命令集合（依序執行）。
        /// </summary>
        public DbCommandSpecCollection Commands { get; set; } = new DbCommandSpecCollection();

        /// <summary>
        /// 是否使用交易包覆整個批次（任何一筆失敗就回滾；成功則提交）。
        /// </summary>
        public bool UseTransaction { get; set; } = false;

        /// <summary>
        /// 交易隔離等級（當 <see cref="UseTransaction"/> 為 true 時可指定）。
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }
    }
}
