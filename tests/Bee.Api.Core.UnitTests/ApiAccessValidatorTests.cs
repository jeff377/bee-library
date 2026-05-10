using System.ComponentModel;
using Bee.Api.Core.Validator;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.UnitTests
{
    [Collection("Initialize")]
    public class ApiAccessValidatorTests
    {
        private sealed class FakeTokenProvider : IAccessTokenValidator
        {
            public bool Result { get; init; }
            public bool Validate(Guid accessToken) => Result;
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
            var original = BackendInfo.AccessTokenValidator;
            try
            {
                BackendInfo.AccessTokenValidator = new FakeTokenProvider { Result = false };
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
                BackendInfo.AccessTokenValidator = original;
            }
        }

        [Fact]
        [DisplayName("ValidateAccess 於 Authenticated 要求且 provider 回傳 true 時應通過")]
        public void ValidateAccess_Authenticated_ValidToken_Succeeds()
        {
            var original = BackendInfo.AccessTokenValidator;
            try
            {
                BackendInfo.AccessTokenValidator = new FakeTokenProvider { Result = true };
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
                BackendInfo.AccessTokenValidator = original;
            }
        }

        [Fact]
        [DisplayName("ValidateAccess 於 AccessToken 非 Empty 但 provider 未設定時應拋 InvalidOperationException")]
        public void ValidateAccess_Authenticated_ProviderNotConfigured_Throws()
        {
            var original = BackendInfo.AccessTokenValidator;
            try
            {
                BackendInfo.AccessTokenValidator = null!;
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
                BackendInfo.AccessTokenValidator = original;
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

        [Fact]
        [DisplayName("ValidateAccess 繼承 Base Method 的屬性：Override 未標記時應使用基底方法屬性")]
        public void ValidateAccess_BaseMethodAttribute_InheritedByOverride_Succeeds()
        {
            var method = typeof(DerivedApi).GetMethod(nameof(DerivedApi.Method_Override));
            var context = new ApiCallContext
            {
                Format = PayloadFormat.Plain,
                IsLocalCall = false,
                AccessToken = Guid.Empty
            };

            var ex = Record.Exception(() => ApiAccessValidator.ValidateAccess(method!, context));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("ValidateAccess Class 層級屬性：方法未標記時應使用 Class 屬性")]
        public void ValidateAccess_ClassLevelAttribute_UsedWhenMethodHasNone_Succeeds()
        {
            var method = typeof(ClassLevelApi).GetMethod(nameof(ClassLevelApi.Method_NoAttribute));
            var context = new ApiCallContext
            {
                Format = PayloadFormat.Plain,
                IsLocalCall = false,
                AccessToken = Guid.Empty
            };

            var ex = Record.Exception(() => ApiAccessValidator.ValidateAccess(method!, context));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("ValidateAccess 方法屬性應覆蓋 Class 層級屬性")]
        public void ValidateAccess_MethodAttributeOverridesClassAttribute_MethodWins()
        {
            // Class 層級為 Public，方法標記為 Encrypted；Plain 傳輸應因方法屬性而被拒
            var method = typeof(ClassLevelApi).GetMethod(nameof(ClassLevelApi.Method_WithAttribute));
            var context = new ApiCallContext
            {
                Format = PayloadFormat.Plain,
                IsLocalCall = false,
                AccessToken = Guid.Empty
            };

            Assert.Throws<UnauthorizedAccessException>(() =>
                ApiAccessValidator.ValidateAccess(method!, context));
        }

        [Fact]
        [DisplayName("ValidateAccess LocalOnly API 遠端呼叫應拋 UnauthorizedAccessException")]
        public void ValidateAccess_LocalOnlyApi_RemoteCall_Throws()
        {
            var method = typeof(DummyApi).GetMethod(nameof(DummyApi.Method_LocalOnly));
            var context = new ApiCallContext
            {
                Format = PayloadFormat.Plain,
                IsLocalCall = false,
                AccessToken = Guid.Empty
            };

            Assert.Throws<UnauthorizedAccessException>(() =>
                ApiAccessValidator.ValidateAccess(method!, context));
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

        private class BaseApi
        {
            [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
            public virtual void Method_Override() { }
        }

        private class DerivedApi : BaseApi
        {
            // 沒有標記 [ApiAccessControl]，應繼承 BaseApi.Method_Override 的屬性
            public override void Method_Override() { }
        }

        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        private class ClassLevelApi
        {
            // 沒有方法層級屬性，應使用 Class 層級的 Public + Anonymous
            public static void Method_NoAttribute() { }

            // 方法層級屬性覆蓋 Class 層級屬性
            [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Anonymous)]
            public static void Method_WithAttribute() { }
        }
    }
}
