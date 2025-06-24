namespace Bee.Define
{
    /// <summary>
    /// 系統層級業務邏輯物件介面。
    /// </summary>
    public interface ISystemBusinessObject : IBusinessObject
    {
        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        CreateSessionResult CreateSession(CreateSessionArgs args);

        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        GetDefineResult GetDefine(GetDefineArgs args);

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        SaveDefineResult SaveDefine(SaveDefineArgs args);
    }
}
