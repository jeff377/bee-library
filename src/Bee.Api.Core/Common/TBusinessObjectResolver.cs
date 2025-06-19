using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Api.Core
{
    /// <summary>
    /// 業務邏輯物件建立解析器，負責依照 progId 建立對應物件實例。
    /// </summary>
    public class TBusinessObjectResolver : IBusinessObjectResolver
    {
        /// <summary>
        /// 建立指定 progId 的業務邏輯物件實例。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        /// <returns>業務邏輯物件實例。</returns>
        public object CreateBusinessObject(Guid accessToken, string progId)
        {
            if (string.IsNullOrWhiteSpace(progId))
                throw new ArgumentException("ProgId cannot be null or empty.", nameof(progId));

            if (StrFunc.IsEquals(progId, SysProgIds.System))
                return BackendInfo.BusinessObjectProvider.CreateSystemBusinessObject(accessToken);
            else
                return BackendInfo.BusinessObjectProvider.CreateFormBusinessObject(accessToken, progId);
        }
    }
}
