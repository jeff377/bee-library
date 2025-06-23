using System;

namespace Bee.Define
{
    /// <summary>
    /// 業務邏輯物件提供者介面，定義所有 BusinessObject 的取得方式。
    /// </summary>
    public interface IBusinessObjectProvider
    {
        /// <summary>
        /// 建立系統層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        ISystemBusinessObject CreateSystemBusinessObject(Guid accessToken);

        /// <summary>
        /// 建立表單層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        IFormBusinessObject CreateFormBusinessObject(Guid accessToken, string progId);
    }
}
