using System.ComponentModel;
using Bee.Api.Core.Transformers;
using Bee.Base;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiPayloadTransformer 測試。需保存／還原 <see cref="ApiServiceOptions"/> 靜態狀態以避免影響其他測試。
    /// </summary>
    [Collection("SysInfo")]
    public class ApiPayloadTransformerTests
    {
        [Fact]
        [DisplayName("Encode 後 Decode 應還原原始 string 物件")]
        public void EncodeDecode_String_RoundTrip()
        {
            var transformer = new ApiPayloadTransformer();
            const string original = "哈囉,世界";

            byte[] encoded = transformer.Encode(original, typeof(string));
            var decoded = transformer.Decode(encoded, typeof(string));

            Assert.Equal(original, decoded);
        }

        [Fact]
        [DisplayName("Encode 傳入 null 應拋出 ArgumentNullException")]
        public void Encode_NullPayload_ThrowsArgumentNullException()
        {
            var transformer = new ApiPayloadTransformer();

            Assert.Throws<ArgumentNullException>(() => transformer.Encode(null!, typeof(string)));
        }

        [Fact]
        [DisplayName("Decode 傳入 null 應拋出 ArgumentNullException")]
        public void Decode_NullPayload_ThrowsArgumentNullException()
        {
            var transformer = new ApiPayloadTransformer();

            Assert.Throws<ArgumentNullException>(() => transformer.Decode(null!, typeof(string)));
        }

        [Fact]
        [DisplayName("Decode 傳入非 byte 陣列應拋出 InvalidOperationException")]
        public void Decode_NonByteArray_ThrowsInvalidOperationException()
        {
            var transformer = new ApiPayloadTransformer();

            var ex = Assert.Throws<InvalidOperationException>(() => transformer.Decode("not-bytes", typeof(string)));
            Assert.IsType<InvalidCastException>(ex.InnerException);
        }

        [Fact]
        [DisplayName("Encrypt 與 Decrypt 於 NoEncryptionEncryptor 下應回傳原始 byte")]
        public void EncryptDecrypt_NoEncryption_ReturnsSameBytes()
        {
            var originalEncryptor = ApiServiceOptions.PayloadEncryptor;
            var originalDebugMode = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = true;
                ApiServiceOptions.PayloadEncryptor = new NoEncryptionEncryptor();
                var transformer = new ApiPayloadTransformer();
                var raw = new byte[] { 1, 2, 3, 4 };
                var key = new byte[] { 9, 9, 9 };

                var encrypted = transformer.Encrypt(raw, key);
                var decrypted = transformer.Decrypt(encrypted, key);

                Assert.Same(raw, encrypted);
                Assert.Same(raw, decrypted);
            }
            finally
            {
                ApiServiceOptions.PayloadEncryptor = originalEncryptor;
                SysInfo.IsDebugMode = originalDebugMode;
            }
        }

        [Fact]
        [DisplayName("Encrypt 傳入 null rawBytes 應拋出 ArgumentNullException")]
        public void Encrypt_NullBytes_ThrowsArgumentNullException()
        {
            var transformer = new ApiPayloadTransformer();

            Assert.Throws<ArgumentNullException>(() => transformer.Encrypt(null!, new byte[] { 1 }));
        }

        [Fact]
        [DisplayName("Decrypt 傳入 null encryptedBytes 應拋出 ArgumentNullException")]
        public void Decrypt_NullBytes_ThrowsArgumentNullException()
        {
            var transformer = new ApiPayloadTransformer();

            Assert.Throws<ArgumentNullException>(() => transformer.Decrypt(null!, new byte[] { 1 }));
        }
    }
}
