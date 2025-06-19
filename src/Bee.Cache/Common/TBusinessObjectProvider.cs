using Bee.Define;
using System;

namespace Bee.Cache
{
    /// <summary>
    /// 業務邏輯物件提供者。
    /// </summary>
    public class TBusinessObjectProvider : IBusinessObjectProvider
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public TBusinessObjectProvider()
        { }

        /// <summary>
        /// 建立系統層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public object CreateSystemObject(Guid accessToken)
        {
            return SysFunc.CreateSystemObject(accessToken);
        }

        /// <summary>
        /// 建立表單層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        public object CreateFormObject(Guid accessToken, string progId)
        {
            return SysFunc.CreateBusinessObject(accessToken, progId);
        }
    }
}
