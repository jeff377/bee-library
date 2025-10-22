using Bee.Define;
using System;

namespace Bee.Business
{
    /// <summary>
    /// 業務邏輯物件提供者。
    /// </summary>
    public class BusinessObjectProvider : IBusinessObjectProvider
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public BusinessObjectProvider()
        { }

        /// <summary>
        /// 建立系統層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        public object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
        {
            return new SystemBusinessObject(accessToken, isLocalCall);
        }

        /// <summary>
        /// 建立表單層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        public object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
        {
            return new FormBusinessObject(accessToken, progId, isLocalCall);
        }
    }
}
