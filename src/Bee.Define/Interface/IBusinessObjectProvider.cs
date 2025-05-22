using System;

namespace Bee.Define
{
    /// <summary>
    /// 業務邏輯物件提供者介面。
    /// </summary>
    public interface IBusinessObjectProvider
    {
        /// <summary>
        /// 建立系統層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        object CreateSystemObject(Guid accessToken);

        /// <summary>
        /// 建立功能層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        object CreateBusinessObject(Guid accessToken, string progID);
    }
}
