using System.ComponentModel;
using Bee.Api.Core.Validator;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

namespace Bee.Api.Core.UnitTests
{
    [Collection("Initialize")]
    public class ApiAccessValidatorTests
    {
        private sealed class FakeTokenProvider : IAccessTokenValidationProvider
        {
            public bool Result { get; init; }
            public bool ValidateAccessToken(Guid accessToken) => Result;
        }

        [Fact]
        [DisplayName("ValidateAccess 於方法未標記 ApiAccessControl 時應拋 UnauthorizedAccessException")]
        public void ValidateAccess_NoAttribute_Throws()
        {
            var method = typeof(DummyApi).GetMethod(nameof(DummyApi.Method_NoAttribute));
            var context = new ApiCallContext
            {
                Format = PayloadFormat.Encrypted,
                IsLocalCall = false,
                AccessToken = Guid.NewGuid()
            };

            Assert.Throws<UnauthorizedAccessException>(() =>
                ApiAccessValidator.ValidateAccess(method!, context));
        }

        [Fact]
        [DisplayName("ValidateAccess 於 Authenticated 要求但 AccessToken 為 Empty 時應拋")]
        public void ValidateAccess_Authenticated_EmptyToken_Throws()
        {
            var method = typeof(DummyApi).GetMethod(nameof(DummyApi.Method_Authenticated));
            var context = new ApiCallContext
            {
                Format = PayloadFormat.Encrypted,
                IsLocalCall = false,
                AccessToken = Guid.Empty
            };

            Assert.Throws<UnauthorizedAccessException>(() =>
                ApiAccessValidator.ValidateAccess(method!, context));
        }

        [Fact]
        [DisplayName("ValidateAccess 於 Authenticated 要求且 provider 回傳 false 時應拋")]
        public void ValidateAccess_Authenticated_InvalidToken_Throws()
        {
            var original = BackendInfo.AccessTokenValidationProvider;
            try
            {
                BackendInfo.AccessTokenValidationProvider = new FakeTokenProvider { Result = false };
                var method = typeof(DummyApi).GetMethod(nameof(DummyApi.Method_Authenticated));
                var context = new ApiCallContext
                {
                    Format = PayloadFormat.Encrypted,
                    IsLocalCall = false,
                    AccessToken = Guid.NewGuid()
                };

                Assert.Throws<UnauthorizedAccessException>(() =>
                    ApiAccessValidator.ValidateAccess(method!, context));
            }
            finally
            {
                BackendInfo.AccessTokenValidationProvider = original;
            }
        }

        [Fact]
        [DisplayName("ValidateAccess 於 Authenticated 要求且 provider 回傳 true 時應通過")]
        public void ValidateAccess_Authenticated_ValidToken_Succeeds()
        {
            var original = BackendInfo.AccessTokenValidationProvider;
            try
            {
                BackendInfo.AccessTokenValidationProvider = new FakeTokenProvider { Result = true };
                var method = typeof(DummyApi).GetMethod(nameof(DummyApi.Method_Authenticated));
                var context = new ApiCallContext
                {
                    Format = PayloadFormat.Encrypted,
                    IsLocalCall = false,
                    AccessToken = Guid.NewGuid()
                };

                var ex = Record.Exception(() => ApiAccessValidator.ValidateAccess(method!, context));
                Assert.Null(ex);
            }
            finally
            {
                BackendInfo.AccessTokenValidationProvider = original;
            }
        }

        [Fact]
        [DisplayName("ValidateAccess 於 AccessToken 非 Empty 但 provider 未設定時應拋 InvalidOperationException")]
        public void ValidateAccess_Authenticated_ProviderNotConfigured_Throws()
        {
            var original = BackendInfo.AccessTokenValidationProvider;
            try
            {
                BackendInfo.AccessTokenValidationProvider = null!;
                var method = typeof(DummyApi).GetMethod(nameof(DummyApi.Method_Authenticated));
                var context = new ApiCallContext
                {
                    Format = PayloadFormat.Encrypted,
                    IsLocalCall = false,
                    AccessToken = Guid.NewGuid()
                };

                Assert.Throws<InvalidOperationException>(() =>
                    ApiAccessValidator.ValidateAccess(method!, context));
            }
            finally
            {
                BackendInfo.AccessTokenValidationProvider = original;
            }
        }

        [Theory]
        [DisplayName("ValidateAccess 依保護等級與傳輸格式正確驗證存取權限")]
        [InlineData(ApiProtectionLevel.Public, PayloadFormat.Plain, true)]                      // 遠端呼叫 Public API，使用 Plain 傳輸 → ✅ 允許
        [InlineData(ApiProtectionLevel.Encoded, PayloadFormat.Encoded, true)]                  // 遠端呼叫 Encoded API，使用 Encoded 傳輸 → ✅ 允許
        [InlineData(ApiProtectionLevel.Encoded, PayloadFormat.Plain, false)]                   // 遠端呼叫 Encoded API，使用 Plain 傳輸 → ❌ 拒絕（缺少編碼）
        [InlineData(ApiProtectionLevel.Encrypted, PayloadFormat.Encrypted, true)]              // 遠端呼叫 Encrypted API，使用 Encrypted 傳輸 → ✅ 允許
        [InlineData(ApiProtectionLevel.Encrypted, PayloadFormat.Encoded, false)]               // 遠端呼叫 Encrypted API，使用 Encoded 傳輸 → ❌ 拒絕（缺少加密）
        [InlineData(ApiProtectionLevel.LocalOnly, PayloadFormat.Plain, true, true)]            // 近端呼叫 LocalOnly API，使用 Plain 傳輸 → ✅ 允許（本機無格式限制）
        [InlineData(ApiProtectionLevel.Encrypted, PayloadFormat.Plain, true, true)]            // 近端呼叫 Encrypted API，使用 Plain 傳輸 → ✅ 允許（本機不受加密限制）
        [InlineData(ApiProtectionLevel.Encoded, PayloadFormat.Plain, true, true)]              // 近端呼叫 Encoded API，使用 Plain 傳輸 → ✅ 允許（本機不受編碼限制）
        [InlineData(ApiProtectionLevel.Public, PayloadFormat.Plain, true, true)]               // 近端呼叫 Public API，使用 Plain 傳輸 → ✅ 允許
        public void ValidateAccess_VariousFormats_ValidatesCorrectly(
            ApiProtectionLevel protectionLevel,
            PayloadFormat format,
            bool expectedSuccess,
            bool isLocal = false)
        {
            // Arrange: 依 protectionLevel 抓對應的方法
            var method = protectionLevel switch
            {
                ApiProtectionLevel.Public => typeof(DummyApi).GetMethod(nameof(DummyApi.Method_Public)),
                ApiProtectionLevel.Encoded => typeof(DummyApi).GetMethod(nameof(DummyApi.Method_Encoded)),
                ApiProtectionLevel.Encrypted => typeof(DummyApi).GetMethod(nameof(DummyApi.Method_Encrypted)),
                ApiProtectionLevel.LocalOnly => typeof(DummyApi).GetMethod(nameof(DummyApi.Method_LocalOnly)),
                _ => throw new InvalidOperationException("Unknown protection level")
            };

            var context = new ApiCallContext
            {
                Format = format,
                IsLocalCall = isLocal,
                AccessToken = Guid.NewGuid() // 模擬有效的 AccessToken
            };

            // Act & Assert
            if (expectedSuccess)
            {
                var ex = Record.Exception(() => ApiAccessValidator.ValidateAccess(method!, context));
                Assert.Null(ex);
            }
            else
            {
                Assert.Throws<UnauthorizedAccessException>(() =>
                    ApiAccessValidator.ValidateAccess(method!, context));
            }
        }

        private class DummyApi
        {
            [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
            public static void Method_Public() { }

            [ApiAccessControl(ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous)]
            public static void Method_Encoded() { }

            [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Anonymous)]
            public static void Method_Encrypted() { }

            [ApiAccessControl(ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Anonymous)]
            public static void Method_LocalOnly() { }

            public static void Method_NoAttribute() { }

            [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
            public static void Method_Authenticated() { }
        }
    }
}
