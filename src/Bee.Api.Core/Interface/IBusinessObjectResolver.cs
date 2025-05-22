using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 商業物件建立介面，負責依照 progID 建立對應物件實例。
    /// </summary>
    public interface IBusinessObjectResolver
    {
        /// <summary>
        /// 建立指定 progID 的業務邏輯物件實例。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        /// <returns>業務邏輯物件實例。</returns>
        object CreateBusinessObject(Guid accessToken, string progID);
    }

}
