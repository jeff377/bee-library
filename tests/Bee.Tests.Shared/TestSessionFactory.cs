using Bee.Definition.Identity;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 測試專用的 Session 輔助工具。
    /// 在指定 <see cref="ISessionInfoService"/>（透過 <see cref="BeeTestFixture"/>，
    /// 或舊版 <see cref="BeeTestServices.GetRequiredService{T}"/> 取得的 process-wide 實例）
    /// 內植入一個有效 SessionInfo，讓需要 AccessToken 的測試不必走 Login 流程
    /// （預設 <c>AuthenticateUser</c> 回傳 false）。
    /// </summary>
    public static class TestSessionFactory
    {
        /// <summary>
        /// 建立一個有效的測試 AccessToken，並將對應的 SessionInfo 寫入指定 fixture 的 SessionInfoService。
        /// Phase 5 推薦用法 — per-class fixture 自帶 ISessionInfoService instance。
        /// </summary>
        /// <param name="fixture">承載目標 SessionInfoService 的 fixture。</param>
        /// <param name="userId">使用者帳號，預設為 "test"。</param>
        /// <param name="expiresIn">有效時間，預設 1 小時。</param>
        /// <returns>有效的 AccessToken。</returns>
        public static Guid CreateAccessToken(BeeTestFixture fixture, string userId = "test", TimeSpan? expiresIn = null)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            return CreateAccessToken(fixture.GetRequiredService<ISessionInfoService>(), userId, expiresIn);
        }

        /// <summary>
        /// 建立一個有效的測試 AccessToken，並將對應的 SessionInfo 寫入 process-wide
        /// <see cref="BeeTestServices.Provider"/> 取得的 SessionInfoService。
        /// </summary>
        /// <param name="userId">使用者帳號，預設為 "test"。</param>
        /// <param name="expiresIn">有效時間，預設 1 小時。</param>
        /// <returns>有效的 AccessToken。</returns>
        /// <remarks>
        /// Phase 5 過渡 overload —— 仍依賴 <see cref="BeeTestServices"/> static holder。新測試請改用
        /// <see cref="CreateAccessToken(BeeTestFixture, string, TimeSpan?)"/>。所有 caller 遷移完成後將於
        /// PR 5.5 移除。
        /// </remarks>
        public static Guid CreateAccessToken(string userId = "test", TimeSpan? expiresIn = null)
            => CreateAccessToken(BeeTestServices.GetRequiredService<ISessionInfoService>(), userId, expiresIn);

        private static Guid CreateAccessToken(ISessionInfoService sessionService, string userId, TimeSpan? expiresIn)
        {
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
