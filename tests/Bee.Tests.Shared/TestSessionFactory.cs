using Bee.Definition.Identity;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 測試專用的 Session 輔助工具。
    /// 在指定 <see cref="BeeTestFixture"/> 的 <see cref="ISessionInfoService"/> 內植入一個有效
    /// SessionInfo，讓需要 AccessToken 的測試不必走 Login 流程（預設 <c>AuthenticateUser</c>
    /// 回傳 false）。
    /// </summary>
    public static class TestSessionFactory
    {
        /// <summary>
        /// 建立一個有效的測試 AccessToken，並將對應的 SessionInfo 寫入指定 fixture 的 SessionInfoService。
        /// </summary>
        /// <param name="fixture">承載目標 SessionInfoService 的 fixture。</param>
        /// <param name="userId">使用者帳號，預設為 "test"。</param>
        /// <param name="expiresIn">有效時間，預設 1 小時。</param>
        /// <returns>有效的 AccessToken。</returns>
        public static Guid CreateAccessToken(BeeTestFixture fixture, string userId = "test", TimeSpan? expiresIn = null)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            var sessionService = fixture.GetRequiredService<ISessionInfoService>();
            var accessToken = Guid.NewGuid();
            sessionService.Set(new SessionInfo
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
