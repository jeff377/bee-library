namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// 表單儲存庫提供者的介面。
    /// </summary>
    public interface IFormRepositoryProvider
    {
        /// <summary>
        /// 依據 ProgId 取得對應的 IDataFormRepository。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        IDataFormRepository GetDataFormRepository(string progId);

        /// <summary>
        /// 依據 ProgId 取得對應的 IReportFormRepository。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        IReportFormRepository GetReportFormRepository(string progId);
    }
}
