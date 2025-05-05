namespace Bee.Api.Core
{
    /// <summary>
    /// 全域 API 驗證註冊器。
    /// </summary>
    public static class ApiAuthorizationValidatorProvider
    {
        private static IApiAuthorizationValidator _validator;

        /// <summary>
        /// 註冊全域 API 驗證器。
        /// </summary>
        /// <param name="validator"></param>
        public static void Register(IApiAuthorizationValidator validator)
        {
            _validator = validator;
        }

        /// <summary>
        /// 取得全域 API 驗證器。
        /// </summary>
        public static IApiAuthorizationValidator GetValidator()
        {
            // 如果沒有註冊的驗證器，則使用預設的驗證器
            if (_validator == null)
            {
               return new TApiAuthorizationValidator();
            }
            return _validator;
        }
    }
}
