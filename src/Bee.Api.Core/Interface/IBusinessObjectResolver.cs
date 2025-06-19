using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 商業物件建立介面，負責依照 progId 建立對應物件實例。
    /// </summary>
    public interface IBusinessObjectResolver
    {
        /// <summary>
        /// 建立指定 progId 的業務邏輯物件實例。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        /// <returns>業務邏輯物件實例。</returns>
        object CreateBusinessObject(Guid accessToken, string progId);
    }

}
