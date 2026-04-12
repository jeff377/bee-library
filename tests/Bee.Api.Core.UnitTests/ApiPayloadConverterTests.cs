using System.ComponentModel;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiPayloadConverter TypeName 白名單驗證測試
    /// </summary>
    [Collection("Initialize")]
    public class ApiPayloadConverterTests
    {
        [Theory]
        [InlineData("Bee.Api.Contracts.System.LoginArgs, Bee.Api.Contracts")]
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
                TypeName = null
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded));

            Assert.Contains("TypeName is missing", ex.Message);
        }
    }
}
