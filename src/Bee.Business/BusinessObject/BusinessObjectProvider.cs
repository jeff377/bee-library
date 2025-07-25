﻿using Bee.Define;
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
        public ISystemBusinessObject CreateSystemBusinessObject(Guid accessToken)
        {
            return new SystemBusinessObject(accessToken);
        }

        /// <summary>
        /// 建立表單層級業務邏輯物件。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="progId">程式代碼。</param>
        public IFormBusinessObject CreateFormBusinessObject(Guid accessToken, string progId)
        {
            return new FormBusinessObject(accessToken, progId);
        }
    }
}
