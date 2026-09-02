using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Transformers;
using Bee.Definition.Collections;
using Bee.Definition.Settings;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 每個請求協商 body codec 的測試：JSON codec 的來回、object 成員的型別保真，
    /// 以及未啟用 / 格式不合的 codec 名稱如何被拒。
    /// </summary>
    /// <remarks>
    /// 會改寫 <see cref="ApiServiceOptions"/> 的 process-wide 靜態元件，故列入
    /// <c>ApiServiceOptionsState</c>（整組件已 <c>DisableTestParallelization</c>，
    /// 此標記用於標示「本類會改寫靜態元件」）。
    /// </remarks>
    [Collection("ApiServiceOptionsState")]
    public class JsonPayloadCodecTests
    {
        /// <summary>
        /// 把 payload 管線設回框架預設（messagepack / gzip / aes-cbc-hmac），並回傳可還原原狀的
        /// disposable。json codec 本身不需要啟用——兩種 codec 恆可用。
        /// </summary>
        private static IDisposable UseDefaultPipeline()
        {
            var originalSerializer = ApiServiceOptions.PayloadSerializer;
            var originalCompressor = ApiServiceOptions.PayloadCompressor;
            var originalEncryptor = ApiServiceOptions.PayloadEncryptor;

            ApiServiceOptions.Initialize(
                new ApiPayloadOptions
                {
                    Compressor = "gzip",
                    Encryptor = "aes-cbc-hmac"
                },
                isDebugMode: true);

            return new Restore(() => ApiServiceOptions.Initialize(
                originalSerializer, originalCompressor, originalEncryptor));
        }

        private sealed class Restore(Action action) : IDisposable
        {
            public void Dispose() => action();
        }

        [Fact]
        [DisplayName("宣告 json codec 的 Encoded payload 應能原樣來回")]
        public void TransformTo_JsonCodec_RoundTrips()
        {
            using var _ = UseDefaultPipeline();
            var payload = new JsonRpcParams
            {
                Codec = PayloadCodecNames.Json,
                Value = new Parameter("greeting", "hello")
            };

            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded);
            Assert.IsType<byte[]>(payload.Value);
            Assert.Equal(PayloadCodecNames.Json, payload.Codec);

            ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded);

            var restored = Assert.IsType<Parameter>(payload.Value);
            Assert.Equal("greeting", restored.Name);
            Assert.Equal("hello", restored.Value);
        }

        [Theory]
        [DisplayName("json codec 的 object 成員應保住原型別，不退化為 JSON 的預設對應")]
        [InlineData(12.5)]          // decimal：JSON number 會退化為 double
        [InlineData(9007199254740993L)] // long：超過 2^53，JSON number 會失精度
        public void TransformTo_JsonCodec_PreservesObjectMemberType(object value)
        {
            // InlineData 無法直接給 decimal，這裡把 double 個案轉回 decimal 再驗。
            object original = value is double d ? (decimal)d : value;

            using var _ = UseDefaultPipeline();
            var payload = new JsonRpcParams
            {
                Codec = PayloadCodecNames.Json,
                Value = new Parameter("amount", original)
            };

            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded);
            ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded);

            var restored = Assert.IsType<Parameter>(payload.Value);
            Assert.Equal(original.GetType(), restored.Value!.GetType());
            Assert.Equal(original, restored.Value);
        }

        [Fact]
        [DisplayName("json codec 的 object 成員為 Guid 時應還原為 Guid 而非字串")]
        public void TransformTo_JsonCodec_PreservesGuidObjectMember()
        {
            using var _ = UseDefaultPipeline();
            var id = Guid.NewGuid();
            var payload = new JsonRpcParams
            {
                Codec = PayloadCodecNames.Json,
                Value = new Parameter("id", id)
            };

            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded);
            ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded);

            var restored = Assert.IsType<Parameter>(payload.Value);
            Assert.Equal(id, Assert.IsType<Guid>(restored.Value));
        }

        [Fact]
        [DisplayName("未宣告 codec 的 payload 不應在信封寫入 codec 欄位")]
        public void TransformTo_NoCodec_LeavesCodecBlank()
        {
            using var _ = UseDefaultPipeline();
            var payload = new JsonRpcParams { Value = new Parameter("greeting", "hello") };

            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded);

            Assert.Equal(string.Empty, payload.Codec);
        }

        [Fact]
        [DisplayName("ResolvePayloadSerializer 對空字串應回傳部署預設 codec")]
        public void ResolvePayloadSerializer_Blank_ReturnsDefault()
        {
            using var _ = UseDefaultPipeline();

            Assert.Same(ApiServiceOptions.PayloadSerializer, ApiServiceOptions.ResolvePayloadSerializer(null));
            Assert.Same(ApiServiceOptions.PayloadSerializer, ApiServiceOptions.ResolvePayloadSerializer(string.Empty));
        }

        [Fact]
        [DisplayName("ResolvePayloadSerializer 應無條件認得兩種內建 codec")]
        public void ResolvePayloadSerializer_BuiltInCodecs_AlwaysResolve()
        {
            using var _ = UseDefaultPipeline();

            Assert.IsType<MessagePackPayloadSerializer>(
                ApiServiceOptions.ResolvePayloadSerializer(PayloadCodecNames.MessagePack));
            Assert.IsType<JsonPayloadSerializer>(
                ApiServiceOptions.ResolvePayloadSerializer(PayloadCodecNames.Json));
        }

        [Fact]
        [DisplayName("ResolvePayloadSerializer 對名稱合法但不存在的 codec 應拒絕")]
        public void ResolvePayloadSerializer_UnknownCodec_Throws()
        {
            using var _ = UseDefaultPipeline();

            var ex = Assert.Throws<NotSupportedException>(
                () => ApiServiceOptions.ResolvePayloadSerializer("protobuf"));
            Assert.Contains("protobuf", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [DisplayName("ResolvePayloadSerializer 對格式不合的 codec 名稱應拒絕且不回顯內容")]
        [InlineData("JSON")]
        [InlineData("json; DROP")]
        [InlineData("json\nInjected: header")]
        [InlineData("../../etc/passwd")]
        public void ResolvePayloadSerializer_MalformedName_ThrowsWithoutEchoing(string codec)
        {
            using var _ = UseDefaultPipeline();

            var ex = Assert.Throws<NotSupportedException>(
                () => ApiServiceOptions.ResolvePayloadSerializer(codec));

            // 名稱來自 wire，錯誤訊息不得把它原樣帶出去。
            Assert.DoesNotContain(codec, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("以 messagepack 編碼卻宣告 json 的 body 應解碼失敗，不得靜默產生預設值")]
        public void RestoreFrom_MessagePackBodyDeclaredAsJson_Fails()
        {
            using var _ = UseDefaultPipeline();
            var payload = new JsonRpcParams { Value = new Parameter("greeting", "hello") };

            // 以預設 codec（messagepack）編碼……
            ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded);
            // ……再謊稱 body 是 json。
            payload.Codec = PayloadCodecNames.Json;

            Assert.ThrowsAny<Exception>(
                () => ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded));
        }
    }
}
