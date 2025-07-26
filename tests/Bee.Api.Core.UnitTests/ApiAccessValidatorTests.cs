using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    [Collection("Initialize")]
    public class ApiAccessValidatorTests
    {
        [Theory]
        [InlineData(ApiProtectionLevel.Public, PayloadFormat.Plain, true)]                      // 遠端呼叫 Public API，使用 Plain 傳輸 → ✅ 允許
        [InlineData(ApiProtectionLevel.Encoded, PayloadFormat.Encoded, true)]                  // 遠端呼叫 Encoded API，使用 Encoded 傳輸 → ✅ 允許
        [InlineData(ApiProtectionLevel.Encoded, PayloadFormat.Plain, false)]                   // 遠端呼叫 Encoded API，使用 Plain 傳輸 → ❌ 拒絕（缺少編碼）
        [InlineData(ApiProtectionLevel.Encrypted, PayloadFormat.Encrypted, true)]              // 遠端呼叫 Encrypted API，使用 Encrypted 傳輸 → ✅ 允許
        [InlineData(ApiProtectionLevel.Encrypted, PayloadFormat.Encoded, false)]               // 遠端呼叫 Encrypted API，使用 Encoded 傳輸 → ❌ 拒絕（缺少加密）
        [InlineData(ApiProtectionLevel.LocalOnly, PayloadFormat.Plain, true, true)]            // 近端呼叫 LocalOnly API，使用 Plain 傳輸 → ✅ 允許（本機無格式限制）
        [InlineData(ApiProtectionLevel.Encrypted, PayloadFormat.Plain, true, true)]            // 近端呼叫 Encrypted API，使用 Plain 傳輸 → ✅ 允許（本機不受加密限制）
        [InlineData(ApiProtectionLevel.Encoded, PayloadFormat.Plain, true, true)]              // 近端呼叫 Encoded API，使用 Plain 傳輸 → ✅ 允許（本機不受編碼限制）
        [InlineData(ApiProtectionLevel.Public, PayloadFormat.Plain, true, true)]               // 近端呼叫 Public API，使用 Plain 傳輸 → ✅ 允許
        public void ValidateAccess_WithVariousFormats_ValidatesCorrectly(
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
                IsLocalCall = isLocal
            };

            // Act & Assert
            if (expectedSuccess)
            {
                var ex = Record.Exception(() => ApiAccessValidator.ValidateAccess(method, context));
                Assert.Null(ex);
            }
            else
            {
                Assert.Throws<UnauthorizedAccessException>(() =>
                    ApiAccessValidator.ValidateAccess(method, context));
            }
        }

        private class DummyApi
        {
            [ApiAccessControl(ApiProtectionLevel.Public)]
            public void Method_Public() { }

            [ApiAccessControl(ApiProtectionLevel.Encoded)]
            public void Method_Encoded() { }

            [ApiAccessControl(ApiProtectionLevel.Encrypted)]
            public void Method_Encrypted() { }

            [ApiAccessControl(ApiProtectionLevel.LocalOnly)]
            public void Method_LocalOnly() { }
        }
    }

}

