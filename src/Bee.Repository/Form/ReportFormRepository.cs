using Bee.Repository.Abstractions;

namespace Bee.Repository
{
    /// <summary>
    /// 報表表單儲存庫。
    /// </summary>
    public class ReportFormRepository : IReportFormRepository
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public ReportFormRepository(string progId)
        {
            ProgId = progId;
        }

        /// <summary>
        /// 程式代碼。
        /// </summary>
        public string ProgId { get; }
    }
}
