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
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true);

        /// <summary>
        /// 建立表單層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true);
    }
}
