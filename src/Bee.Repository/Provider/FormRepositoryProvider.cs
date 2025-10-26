using Bee.Repository.Abstractions;
using System;

namespace Bee.Repository
{
    /// <summary>
    /// 表單儲存庫提供者。
    /// </summary>
    public class FormRepositoryProvider : IFormRepositoryProvider
    {
        /// <summary>
        /// 依據 ProgId 取得對應的 IDataFormRepository。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public IDataFormRepository GetDataFormRepository(string progId)
        {
            return new DataFormRepository(progId);
        }

        /// <summary>
        /// 依據 ProgId 取得對應的 IReportFormRepository。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public IReportFormRepository GetReportFormRepository(string progId)
        {
            return new ReportFormRepository(progId);
        }
    }
}
