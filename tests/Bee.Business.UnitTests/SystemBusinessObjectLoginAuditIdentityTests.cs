using System.ComponentModel;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 登入軌跡的呼叫端識別：`st_log_login` 要能分辨「哪個應用在嘗試登入」——同一批失敗來自
    /// 單一應用，與散在數個應用，是完全不同的訊號。
    /// </summary>
    public class SystemBusinessObjectLoginAuditIdentityTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectLoginAuditIdentityTests(SharedDbFixture fx) { _fx = fx; }

        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];

            public void Write(AuditEntry entry) => Entries.Add(entry);
        }

        private static readonly AuditLogOptions LoginAuditEnabled = new()
        {
            Enabled = true,
            LoginEnabled = true,
        };

        /// <summary>
        /// 以「驗證失敗」觸發登入軌跡：不需真實憑證，且失敗正是最需要辨識呼叫端的情境。
        /// </summary>
        private LoginAuditEntry RunFailedLogin(ApiKeyValidationResult validation, CapturingAuditLogWriter writer)
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(IAuditLogWriter), writer),
                (typeof(AuditLogOptions), LoginAuditEnabled));
            var bo = new TestableSystemBusinessObject(ctx, Guid.Empty, _ => (false, string.Empty))
            {
                ApiKeyValidation = validation,
            };

            Assert.Throws<UnauthorizedAccessException>(
                () => bo.Login(new LoginArgs { UserId = "user01", Password = "wrong" }));

            return Assert.IsType<LoginAuditEntry>(Assert.Single(writer.Entries));
        }

        [Fact]
        [DisplayName("登入軌跡應記下呼叫端應用的金鑰識別碼與名稱")]
        public void Login_WithApiKey_RecordsCallingApplication()
        {
            var writer = new CapturingAuditLogWriter();

            var entry = RunFailedLogin(
                new ApiKeyValidationResult(ApiKeyStatus.Valid, "northwind-desktop", "Northwind Desktop"),
                writer);

            Assert.Equal(LoginEvent.LoginFailed, entry.Event);
            Assert.Equal("northwind-desktop", entry.ApiKeyId);
            Assert.Equal("Northwind Desktop", entry.ApiKeyName);
        }

        [Fact]
        [DisplayName("未經金鑰閘門的登入，軌跡的應用識別應為 null 而非空字串")]
        public void Login_WithoutApiKey_LeavesIdentityNull()
        {
            var writer = new CapturingAuditLogWriter();

            var entry = RunFailedLogin(ApiKeyValidationResult.NotChecked, writer);

            Assert.Null(entry.ApiKeyId);
            Assert.Null(entry.ApiKeyName);
        }
    }
}
