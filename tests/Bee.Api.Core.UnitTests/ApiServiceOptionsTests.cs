using System.ComponentModel;
using Bee.Api.Core.Authorization;
using Bee.Api.Core.Transformers;
using Bee.Base;
using Bee.Definition.Settings;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiServiceOptions 測試。由於 ApiServiceOptions 為靜態類別，測試會保存／還原原始實作以避免影響其他測試。
    /// </summary>
    public class ApiServiceOptionsTests
    {
        [Fact]
        [DisplayName("預設實作應為對應的內建類別")]
        public void DefaultImplementations_AreBuiltInTypes()
        {
            Assert.IsType<ApiAuthorizationValidator>(ApiServiceOptions.AuthorizationValidator);
            Assert.IsType<ApiPayloadTransformer>(ApiServiceOptions.PayloadTransformer);
            Assert.IsType<MessagePackPayloadSerializer>(ApiServiceOptions.PayloadSerializer);
            Assert.IsType<GzipPayloadCompressor>(ApiServiceOptions.PayloadCompressor);
        }

        [Fact]
        [DisplayName("CurrentSettingsSummary 應包含三項 Method 名稱")]
        public void CurrentSettingsSummary_ContainsMethodNames()
        {
            var summary = ApiServiceOptions.CurrentSettingsSummary;

            Assert.Contains("Serializer:", summary);
            Assert.Contains("Compressor:", summary);
            Assert.Contains("Encryptor:", summary);
        }

        [Fact]
        [DisplayName("Initialize(ApiPayloadOptions) 應依名稱建立對應實作")]
        public void Initialize_WithOptions_SetsImplementations()
        {
            var originalSerializer = ApiServiceOptions.PayloadSerializer;
            var originalCompressor = ApiServiceOptions.PayloadCompressor;
            var originalEncryptor = ApiServiceOptions.PayloadEncryptor;
            var originalDebugMode = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = true;
                var options = new ApiPayloadOptions
                {
                    Serializer = "messagepack",
                    Compressor = "none",
                    Encryptor = "none"
                };

                ApiServiceOptions.Initialize(options);

                Assert.IsType<MessagePackPayloadSerializer>(ApiServiceOptions.PayloadSerializer);
                Assert.IsType<NoCompressionCompressor>(ApiServiceOptions.PayloadCompressor);
                Assert.IsType<NoEncryptionEncryptor>(ApiServiceOptions.PayloadEncryptor);
            }
            finally
            {
                ApiServiceOptions.PayloadSerializer = originalSerializer;
                ApiServiceOptions.PayloadCompressor = originalCompressor;
                ApiServiceOptions.PayloadEncryptor = originalEncryptor;
                SysInfo.IsDebugMode = originalDebugMode;
            }
        }

        [Fact]
        [DisplayName("Initialize(serializer, compressor, encryptor) 應直接採用傳入實作")]
        public void Initialize_WithInstances_SetsImplementations()
        {
            var originalSerializer = ApiServiceOptions.PayloadSerializer;
            var originalCompressor = ApiServiceOptions.PayloadCompressor;
            var originalEncryptor = ApiServiceOptions.PayloadEncryptor;
            try
            {
                var serializer = new MessagePackPayloadSerializer();
                var compressor = new NoCompressionCompressor();
                var encryptor = new NoEncryptionEncryptor();

                ApiServiceOptions.Initialize(serializer, compressor, encryptor);

                Assert.Same(serializer, ApiServiceOptions.PayloadSerializer);
                Assert.Same(compressor, ApiServiceOptions.PayloadCompressor);
                Assert.Same(encryptor, ApiServiceOptions.PayloadEncryptor);
            }
            finally
            {
                ApiServiceOptions.PayloadSerializer = originalSerializer;
                ApiServiceOptions.PayloadCompressor = originalCompressor;
                ApiServiceOptions.PayloadEncryptor = originalEncryptor;
            }
        }

        [Fact]
        [DisplayName("Initialize 傳入 null serializer 應拋出 ArgumentNullException")]
        public void Initialize_NullSerializer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ApiServiceOptions.Initialize(null!, new NoCompressionCompressor(), new NoEncryptionEncryptor()));
        }

        [Fact]
        [DisplayName("Initialize 傳入 null compressor 應拋出 ArgumentNullException")]
        public void Initialize_NullCompressor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ApiServiceOptions.Initialize(new MessagePackPayloadSerializer(), null!, new NoEncryptionEncryptor()));
        }

        [Fact]
        [DisplayName("Initialize 傳入 null encryptor 應拋出 ArgumentNullException")]
        public void Initialize_NullEncryptor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ApiServiceOptions.Initialize(new MessagePackPayloadSerializer(), new NoCompressionCompressor(), null!));
        }

        [Fact]
        [DisplayName("AuthorizationValidator 設為 null 應拋出 ArgumentNullException")]
        public void AuthorizationValidator_SetNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ApiServiceOptions.AuthorizationValidator = null!);
        }

        [Fact]
        [DisplayName("PayloadTransformer 設為 null 應拋出 ArgumentNullException")]
        public void PayloadTransformer_SetNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ApiServiceOptions.PayloadTransformer = null!);
        }

        [Fact]
        [DisplayName("PayloadSerializer 設為 null 應拋出 ArgumentNullException")]
        public void PayloadSerializer_SetNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ApiServiceOptions.PayloadSerializer = null!);
        }

        [Fact]
        [DisplayName("PayloadCompressor 設為 null 應拋出 ArgumentNullException")]
        public void PayloadCompressor_SetNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ApiServiceOptions.PayloadCompressor = null!);
        }

        [Fact]
        [DisplayName("PayloadEncryptor 設為 null 應拋出 ArgumentNullException")]
        public void PayloadEncryptor_SetNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ApiServiceOptions.PayloadEncryptor = null!);
        }
    }
}
