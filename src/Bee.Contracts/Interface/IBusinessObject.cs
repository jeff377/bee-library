namespace Bee.Contracts
{
    /// <summary>
    /// 業務邏輯物件基底介面。
    /// </summary>
    public interface IBusinessObject
    {
        /// <summary>
        /// 執行自訂方法，開放方法，要求登入。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        ExecFuncResult ExecFunc(ExecFuncArgs args);

        /// <summary>
        /// 執行自訂方法，開放方法，匿名存取。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        ExecFuncResult ExecFuncAnonymous(ExecFuncArgs args);
    }
}
