using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Transformer;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiPayloadConverter TypeName 白名單驗證測試
    /// </summary>
    [Collection("Initialize")]
    public class ApiPayloadConverterTests
    {
        private static byte[] MakeKey()
        {
            // AES-CBC-HMAC 需要 64 bytes 組合金鑰（32 AES + 32 HMAC）
            var key = new byte[64];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)i;
            return key;
        }

        [Fact]
        [DisplayName("TransformTo Plain 應直接設 Format 並回傳")]
        public void TransformTo_Plain_SetsFormatAndReturns()
        {
            var payload = new JsonRpcParams { Value = "hello" };

            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Plain);

            Assert.Equal(PayloadFormat.Plain, payload.Format);
            Assert.Equal("hello", payload.Value);
        }

        [Fact]
        [DisplayName("TransformTo 於 Encoded 但 Value 為 null 應拋出 InvalidOperationException")]
        public void TransformTo_Encoded_NullValue_Throws()
        {
            var payload = new JsonRpcParams { Value = null };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded));

            Assert.Contains("Payload.Value", ex.Message);
        }

        [Fact]
        [DisplayName("TransformTo 於 Encrypted 但 key 為 null 應拋出 InvalidOperationException")]
        public void TransformTo_Encrypted_NullKey_Throws()
        {
            var payload = new JsonRpcParams { Value = "hello" };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encrypted, null));

            Assert.Contains("Encryption key", ex.Message);
        }

        [Fact]
        [DisplayName("TransformTo 於 Encrypted 但 key 為空陣列應拋出 InvalidOperationException")]
        public void TransformTo_Encrypted_EmptyKey_Throws()
        {
            var payload = new JsonRpcParams { Value = "hello" };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encrypted, Array.Empty<byte>()));

            Assert.Contains("Encryption key", ex.Message);
        }

        [Fact]
        [DisplayName("TransformTo Encoded 後 RestoreFrom Encoded 應還原原始字串")]
        public void TransformTo_Encoded_RoundTrip_RestoresOriginalValue()
        {
            var payload = new JsonRpcParams { Value = "哈囉,世界" };

            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded);

            Assert.Equal(PayloadFormat.Encoded, payload.Format);
            Assert.IsType<byte[]>(payload.Value);
            Assert.False(string.IsNullOrEmpty(payload.TypeName));

            ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded);

            Assert.Equal(PayloadFormat.Plain, payload.Format);
            Assert.Equal("哈囉,世界", payload.Value);
        }

        [Fact]
        [DisplayName("TransformTo Encrypted 後 RestoreFrom Encrypted 應還原原始字串")]
        public void TransformTo_Encrypted_RoundTrip_RestoresOriginalValue()
        {
            var key = MakeKey();
            var payload = new JsonRpcParams { Value = "secret-data" };

            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encrypted, key);

            Assert.Equal(PayloadFormat.Encrypted, payload.Format);
            Assert.IsType<byte[]>(payload.Value);

            ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encrypted, key);

            Assert.Equal(PayloadFormat.Plain, payload.Format);
            Assert.Equal("secret-data", payload.Value);
        }

        [Fact]
        [DisplayName("TransformTo Plain 時即使 key 為 null 也不應拋")]
        public void TransformTo_Plain_NullKey_DoesNotThrow()
        {
            var payload = new JsonRpcParams { Value = "x" };

            var ex = Record.Exception(() =>
                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Plain, null));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("RestoreFrom 於 Value 非 byte[] 應拋出 InvalidCastException")]
        public void RestoreFrom_NonByteArrayValue_ThrowsInvalidCastException()
        {
            var payload = new JsonRpcParams
            {
                Value = "not-bytes",
                TypeName = "System.String"
            };

            Assert.Throws<InvalidCastException>(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded));
        }

        [Fact]
        [DisplayName("RestoreFrom 於 TypeName 無法解析為 Type 應拋出 InvalidOperationException")]
        public void RestoreFrom_UnresolvableTypeName_Throws()
        {
            // 通過白名單 (Bee.Api.Core.*) 但無對應實際型別
            var payload = new JsonRpcParams
            {
                Value = new byte[] { 0x01 },
                TypeName = "Bee.Api.Core.DoesNotExistClass, Bee.Api.Core"
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded));

            Assert.Contains("Unable to load type", ex.Message);
        }

        [Fact]
        [DisplayName("RestoreFrom 於 Encrypted 但 key 為 null 應拋出 InvalidOperationException")]
        public void RestoreFrom_Encrypted_NullKey_Throws()
        {
            var payload = new JsonRpcParams
            {
                Value = new byte[] { 0x01, 0x02 },
                TypeName = "System.String"
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encrypted, null));

            Assert.Contains("encryption key", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [DisplayName("RestoreFrom 於 Encrypted 但 key 為空陣列應拋出 InvalidOperationException")]
        public void RestoreFrom_Encrypted_EmptyKey_Throws()
        {
            var payload = new JsonRpcParams
            {
                Value = new byte[] { 0x01, 0x02 },
                TypeName = "System.String"
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encrypted, Array.Empty<byte>()));

            Assert.Contains("encryption key", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Bee.Api.Core.System.LoginRequest, Bee.Api.Core")]
        [InlineData("Bee.Definition.Collections.ParameterCollection, Bee.Definition")]
        [InlineData("Bee.Base.SomeClass, Bee.Base")]
        [InlineData("Bee.Contracts.SomeDto, Bee.Contracts")]
        [InlineData("System.Int32")]
        [DisplayName("RestoreFrom 應允許白名單內的 TypeName")]
        public void RestoreFrom_AllowedTypeName_DoesNotThrowValidationError(string typeName)
        {
            var payload = new JsonRpcParams
            {
                Value = new byte[] { 0x01 },
                TypeName = typeName
            };

            // The call may throw due to deserialization failure (invalid bytes),
            // but it should NOT throw the whitelist validation error.
            var ex = Record.Exception(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded));

            if (ex != null)
            {
                Assert.DoesNotContain("not in the allowed type whitelist", ex.Message);
            }
        }

        [Theory]
        [InlineData("System.Diagnostics.Process, System.Diagnostics.Process")]
        [InlineData("System.IO.FileInfo, System.IO.FileSystem")]
        [InlineData("Evil.Namespace.Exploit, Evil.Assembly")]
        [InlineData("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter, mscorlib")]
        [DisplayName("RestoreFrom 應拒絕不在白名單內的 TypeName")]
        public void RestoreFrom_DisallowedTypeName_ThrowsInvalidOperationException(string typeName)
        {
            var payload = new JsonRpcParams
            {
                Value = new byte[] { 0x01 },
                TypeName = typeName
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded));

            Assert.Contains("not in the allowed type whitelist", ex.Message);
        }

        [Fact]
        [DisplayName("RestoreFrom Plain 格式不做 TypeName 驗證")]
        public void RestoreFrom_PlainFormat_SkipsValidation()
        {
            var payload = new JsonRpcParams
            {
                Value = "some value",
                TypeName = "Evil.Namespace.Exploit, Evil.Assembly"
            };

            // Plain format should skip all validation and return immediately
            ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Plain);
            Assert.Equal(PayloadFormat.Plain, payload.Format);
        }

        [Fact]
        [DisplayName("RestoreFrom 缺少 TypeName 應拋出例外")]
        public void RestoreFrom_MissingTypeName_ThrowsInvalidOperationException()
        {
            var payload = new JsonRpcParams
            {
                Value = new byte[] { 0x01 },
                TypeName = null!
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded));

            Assert.Contains("TypeName is missing", ex.Message);
        }
    }
}
