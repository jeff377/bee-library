using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Api.Core
{
    /// <summary>
    /// 商業物件建立預設實作，負責依照 progID 建立對應物件實例。
    /// </summary>
    public class TBusinessObjectResolver : IBusinessObjectResolver
    {
        /// <summary>
        /// 建立指定 progID 的商業邏輯物件實例。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        /// <returns>商業邏輯物件實例。</returns>
        public object CreateBusinessObject(Guid accessToken, string progID)
        {
            if (string.IsNullOrWhiteSpace(progID))
                throw new ArgumentException("ProgID cannot be null or empty.", nameof(progID));

            if (StrFunc.IsEquals(progID, SysProgIDs.System))
                return BackendInfo.BusinessObjectProvider.CreateSystemObject(accessToken);
            else
                return BackendInfo.BusinessObjectProvider.CreateBusinessObject(accessToken, progID);
        }
    }
}
