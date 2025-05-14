using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 提供 API 框架可自訂的元件設定。
    /// </summary>
    public static class ApiServiceOptions
    {
        private static IApiAuthorizationValidator _authorizationValidator = new TApiAuthorizationValidator(); // 預設實作
        private static IApiPayloadTransformer _payloadTransformer = new TApiPayloadTransformer(); // 預設實作

        /// <summary>
        /// 設定或取得授權驗證元件。
        /// </summary>
        public static IApiAuthorizationValidator AuthorizationValidator
        {
            get => _authorizationValidator;
            set => _authorizationValidator = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 設定或取得傳輸資料轉換元件。
        /// </summary>
        public static IApiPayloadTransformer PayloadTransformer
        {
            get => _payloadTransformer;
            set => _payloadTransformer = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

}
