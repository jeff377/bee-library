namespace Bee.Define
{
    /// <summary>
    /// 業務邏輯物件基底介面。
    /// </summary>
    public interface IBusinessObject
    {
        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        TExecFuncResult ExecFunc(TExecFuncArgs args);
    }
}
