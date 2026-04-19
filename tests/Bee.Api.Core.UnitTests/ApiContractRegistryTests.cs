using System.ComponentModel;
using Bee.Api.Core.System;
using Bee.Api.Contracts;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// Tests for <see cref="ApiContractRegistry"/> contract mapping functionality.
    /// </summary>
    public class ApiContractRegistryTests
    {
        /// <summary>
        /// Simple POCO that implements ILoginResponse without MessagePack attributes.
        /// </summary>
        private class SimpleLoginResult : ILoginResponse
        {
            public Guid AccessToken { get; set; }
            public DateTime ExpiredAt { get; set; }
            public string ApiEncryptionKey { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
        }

        [Fact]
        [DisplayName("ConvertForSerialization 對已有 MessagePackObject 的型別不做轉換")]
        public void ConvertForSerialization_TypeWithMessagePackObject_ReturnsSameInstance()
        {
            var original = new LoginResponse
            {
                AccessToken = Guid.NewGuid(),
                UserId = "testUser"
            };

            var result = ApiContractRegistry.ConvertForSerialization(original);

            Assert.Same(original, result);
        }

        [Fact]
        [DisplayName("ConvertForSerialization 對 null 回傳 null")]
        public void ConvertForSerialization_NullValue_ReturnsNull()
        {
            var result = ApiContractRegistry.ConvertForSerialization(null!);

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("ConvertForSerialization 將純 POCO 映射為 API 型別")]
        public void ConvertForSerialization_RegisteredPoco_ReturnsApiType()
        {
            // Arrange
            ApiContractRegistry.Register<ILoginResponse, LoginResponse>();

            var poco = new SimpleLoginResult
            {
                AccessToken = Guid.NewGuid(),
                ExpiredAt = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                ApiEncryptionKey = "test-key",
                UserId = "user123",
                UserName = "Test User"
            };

            // Act
            var result = ApiContractRegistry.ConvertForSerialization(poco);

            // Assert
            Assert.IsType<LoginResponse>(result);
            var apiResult = (LoginResponse)result;
            Assert.Equal(poco.AccessToken, apiResult.AccessToken);
            Assert.Equal(poco.ExpiredAt, apiResult.ExpiredAt);
            Assert.Equal(poco.ApiEncryptionKey, apiResult.ApiEncryptionKey);
            Assert.Equal(poco.UserId, apiResult.UserId);
            Assert.Equal(poco.UserName, apiResult.UserName);
        }

        [Fact]
        [DisplayName("ConvertForSerialization 未註冊的型別回傳原始物件")]
        public void ConvertForSerialization_UnregisteredType_ReturnsSameInstance()
        {
            var original = new object();

            var result = ApiContractRegistry.ConvertForSerialization(original);

            Assert.Same(original, result);
        }
    }
}
