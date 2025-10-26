using Bee.Repository.Abstractions;

namespace Bee.Repository
{
    /// <summary>
    /// 資料表單儲存庫。
    /// </summary>
    public class DataFormRepository : IDataFormRepository
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public DataFormRepository(string progId)
        {
            ProgId = progId;
        }

        /// <summary>
        /// 程式代碼。
        /// </summary>
        public string ProgId { get; }
    }
}
