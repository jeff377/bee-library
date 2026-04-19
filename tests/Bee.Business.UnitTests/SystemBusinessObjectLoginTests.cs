using System.ComponentModel;
using Bee.Base.Security;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition;
using Bee.Definition.Security;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject.Login"/> 分支測試，使用 <see cref="TestableSystemBusinessObject"/>
    /// 覆寫 AuthenticateUser 以觸發成功/失敗/鎖定等路徑。
    /// </summary>
    [Collection("Initialize")]
    public class SystemBusinessObjectLoginTests
    {
        private sealed class RecordingTracker : ILoginAttemptTracker
        {
            public bool LockedOut { get; set; }
            public int FailureCount { get; private set; }
            public int ResetCount { get; private set; }

            public bool IsLockedOut(string userId) => LockedOut;
            public void RecordFailure(string userId) => FailureCount++;
            public void Reset(string userId) => ResetCount++;
        }

        [Fact]
        [DisplayName("Login 驗證成功應產生 AccessToken 與到期時間並建立 SessionInfo")]
        public void Login_Authenticated_ReturnsValidSessionToken()
        {
            var bo = new TestableSystemBusinessObject(
                Guid.Empty,
                _ => (true, "User One"));
            var args = new LoginArgs { UserId = "user01", Password = "pwd" };

            var result = bo.Login(args);

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.AccessToken);
            Assert.True(result.ExpiredAt > DateTime.UtcNow);
            Assert.Equal("user01", result.UserId);
            Assert.Equal("User One", result.UserName);
            // 未提供 ClientPublicKey → EncryptedApiEncryptionKey 保持空字串
            Assert.Equal(string.Empty, result.ApiEncryptionKey);

            var session = BackendInfo.SessionInfoService.Get(result.AccessToken);
            try
            {
                Assert.NotNull(session);
                Assert.Equal("user01", session!.UserId);
                Assert.NotEmpty(session.ApiEncryptionKey);
            }
            finally
            {
                BackendInfo.SessionInfoService.Remove(result.AccessToken);
            }
        }

        [Fact]
        [DisplayName("Login 提供 ClientPublicKey 應以 RSA 加密 ApiEncryptionKey")]
        public void Login_WithClientPublicKey_EncryptsApiKey()
        {
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);
            var bo = new TestableSystemBusinessObject(
                Guid.Empty,
                _ => (true, "RSA User"));
            var args = new LoginArgs
            {
                UserId = "rsa_user",
                Password = "x",
                ClientPublicKey = publicKeyXml
            };

            var result = bo.Login(args);

            try
            {
                Assert.False(string.IsNullOrWhiteSpace(result.ApiEncryptionKey));
                var sessionKeyBase64 = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml);
                Assert.False(string.IsNullOrWhiteSpace(sessionKeyBase64));

                var session = BackendInfo.SessionInfoService.Get(result.AccessToken);
                Assert.NotNull(session);
                Assert.Equal(Convert.ToBase64String(session!.ApiEncryptionKey), sessionKeyBase64);
            }
            finally
            {
                BackendInfo.SessionInfoService.Remove(result.AccessToken);
            }
        }

        [Fact]
        [DisplayName("Login 驗證失敗應拋 UnauthorizedAccessException 並記錄 tracker 失敗")]
        public void Login_AuthenticateFails_ThrowsAndRecordsFailure()
        {
            var tracker = new RecordingTracker();
            var original = BackendInfo.LoginAttemptTracker;
            BackendInfo.LoginAttemptTracker = tracker;
            try
            {
                var bo = new TestableSystemBusinessObject(
                    Guid.Empty,
                    _ => (false, string.Empty));
                var args = new LoginArgs { UserId = "bad", Password = "bad" };

                Assert.Throws<UnauthorizedAccessException>(() => bo.Login(args));
                Assert.Equal(1, tracker.FailureCount);
                Assert.Equal(0, tracker.ResetCount);
            }
            finally
            {
                BackendInfo.LoginAttemptTracker = original;
            }
        }

        [Fact]
        [DisplayName("Login 已鎖定帳戶應直接拋 UnauthorizedAccessException 不觸發驗證")]
        public void Login_AccountLockedOut_ThrowsBeforeAuthenticate()
        {
            var tracker = new RecordingTracker { LockedOut = true };
            var authCalls = 0;
            var original = BackendInfo.LoginAttemptTracker;
            BackendInfo.LoginAttemptTracker = tracker;
            try
            {
                var bo = new TestableSystemBusinessObject(
                    Guid.Empty,
                    _ =>
                    {
                        authCalls++;
                        return (true, "anything");
                    });
                var args = new LoginArgs { UserId = "locked", Password = "x" };

                Assert.Throws<UnauthorizedAccessException>(() => bo.Login(args));
                Assert.Equal(0, authCalls);
                Assert.Equal(0, tracker.FailureCount);
            }
            finally
            {
                BackendInfo.LoginAttemptTracker = original;
            }
        }

        [Fact]
        [DisplayName("Login 驗證成功且 tracker 非 null 應呼叫 Reset")]
        public void Login_SuccessWithTracker_CallsReset()
        {
            var tracker = new RecordingTracker();
            var original = BackendInfo.LoginAttemptTracker;
            BackendInfo.LoginAttemptTracker = tracker;
            try
            {
                var bo = new TestableSystemBusinessObject(
                    Guid.Empty,
                    _ => (true, "ok"));
                var args = new LoginArgs { UserId = "u", Password = "p" };

                var result = bo.Login(args);
                try
                {
                    Assert.Equal(1, tracker.ResetCount);
                    Assert.Equal(0, tracker.FailureCount);
                }
                finally
                {
                    BackendInfo.SessionInfoService.Remove(result.AccessToken);
                }
            }
            finally
            {
                BackendInfo.LoginAttemptTracker = original;
            }
        }

        [Fact]
        [DisplayName("SystemBusinessObject 基底 AuthenticateUser 應預設回傳 false")]
        public void BaseAuthenticateUser_DefaultsToFalse()
        {
            // 基底類別未覆寫時 AuthenticateUser 永遠回 false，Login 必拋 UnauthorizedAccessException。
            var bo = new SystemBusinessObject(Guid.Empty);
            var args = new LoginArgs { UserId = "u", Password = "p" };

            Assert.Throws<UnauthorizedAccessException>(() => bo.Login(args));
        }
    }
}
