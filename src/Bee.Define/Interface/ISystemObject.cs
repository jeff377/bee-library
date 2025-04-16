namespace Bee.Define
{
    /// <summary>
    /// 系統層級商業邏輯物件介面。
    /// </summary>
    public interface ISystemObject : IBaseBusinessObject
    {
        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        TCreateSessionResult CreateSession(TCreateSessionArgs args);

        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        TGetDefineResult GetDefine(TGetDefineArgs args);

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        TSaveDefineResult SaveDefine(TSaveDefineArgs args);
    }
}
