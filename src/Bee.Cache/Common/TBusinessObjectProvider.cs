using Bee.Define;
using System;

namespace Bee.Cache
{
    /// <summary>
    /// 商業邏輯物件提供者。
    /// </summary>
    public class TBusinessObjectProvider : IBusinessObjectProvider
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public TBusinessObjectProvider()
        { }

        /// <summary>
        /// 建立系統層級商業邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public ISystemObject CreateSystemObject(Guid accessToken)
        {
            return SysFunc.CreateSystemObject(accessToken);
        }

        /// <summary>
        /// 建立功能層級商業邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progID">程式代碼。</param>
        public IBusinessObject CreateBusinessObject(Guid accessToken, string progID)
        {
            return SysFunc.CreateBusinessObject(accessToken, progID);
        }
    }
}
