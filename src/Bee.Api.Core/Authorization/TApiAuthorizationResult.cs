using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// API 授權驗證結果。
    /// </summary>
    public class TApiAuthorizationResult
    {
        /// <summary>
        /// 是否驗證成功。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 錯誤代碼。
        /// </summary>
        public EJsonRpcErrorCode Code { get; set; }

        /// <summary>
        /// 驗證失敗的錯誤訊息。
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 成功驗證後的存取權杖。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 建立驗證成功的結果。
        /// </summary>
        /// <param name="accessToken">存取權杖。</param>
        public static TApiAuthorizationResult Success(Guid accessToken)
        {
            return new TApiAuthorizationResult
            {
                IsValid = true,
                AccessToken = accessToken
            };
        }

        /// <summary>
        /// 建立驗證失敗的結果。
        /// </summary>
        /// <param name="code">錯誤代碼。</param>
        /// <param name="errorMessage">錯誤訊息。</param>
        public static TApiAuthorizationResult Fail(EJsonRpcErrorCode code,  string errorMessage)
        {
            return new TApiAuthorizationResult
            {
                IsValid = false,
                Code = code,
                ErrorMessage = errorMessage
            };
        }
    }
}
