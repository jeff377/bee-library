using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 測試專用的 Session 輔助工具。
    /// 直接在 <see cref="BackendInfo.SessionInfoService"/> 植入一個有效 SessionInfo，
    /// 讓需要 AccessToken 的測試不必走 Login 流程（預設 <c>AuthenticateUser</c> 回傳 false）。
    /// </summary>
    public static class TestSessionFactory
    {
        /// <summary>
        /// 建立一個有效的測試 AccessToken，並將對應的 SessionInfo 寫入 SessionInfoService。
        /// </summary>
        /// <param name="userId">使用者帳號，預設為 "test"。</param>
        /// <param name="expiresIn">有效時間，預設 1 小時。</param>
        /// <returns>有效的 AccessToken。</returns>
        public static Guid CreateAccessToken(string userId = "test", TimeSpan? expiresIn = null)
        {
            var accessToken = Guid.NewGuid();
            BackendInfo.SessionInfoService.Set(new SessionInfo
            {
                AccessToken = accessToken,
                UserId = userId,
                UserName = userId,
                ExpiredAt = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1)),
                ApiEncryptionKey = Array.Empty<byte>()
            });
            return accessToken;
        }
    }
}
