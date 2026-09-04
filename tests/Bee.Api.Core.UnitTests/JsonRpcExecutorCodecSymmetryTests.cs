using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.System;
using Bee.Api.Core.Transformers;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 守住 codec 協商的核心不變式：<b>伺服端必須以請求宣告的那個 codec 回應</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 整個機制（ADR-044）建立在 <c>JsonRpcExecutor</c> 的一行賦值上：
    /// <c>response.Result = new JsonRpcResult { Value = value, Codec = request.Params.Codec }</c>。
    /// 拿掉那一行，協商過 json 的客戶端會收到 MessagePack 的 body ——
    /// 它解不動，而且**在 2026-09-04 之前整個測試套件仍然全綠**。
    /// </para>
    /// <para>
    /// 既有的 <c>ApiConnectorExecuteTests</c> 看似涵蓋這件事，實際上它的假 provider 是<b>自己</b>
    /// 寫回 <c>Codec = req.Params.Codec</c> 的，證明的是 stub 的行為而非 executor 的；
    /// <c>JsonPayloadCodecTests</c> 則只到 <c>ApiPayloadConverter</c> 那一層，碰不到 executor。
    /// </para>
    /// <para>
    /// 因此本測試斷言兩件事，缺一不可：回應的 <c>Codec</c> 欄與請求相同（宣告面），
    /// 且該 body 真的解得開（實際面）。只驗前者的話，把賦值改成寫死字串仍會過。
    /// </para>
    /// </remarks>
    [Collection("ApiServiceOptionsState")]
    public class JsonRpcExecutorCodecSymmetryTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public JsonRpcExecutorCodecSymmetryTests(BeeTestFixture fx) { _fx = fx; }

        /// <summary>
        /// 把 payload 管線設回框架預設，並回傳可還原原狀的 disposable。
        /// </summary>
        private static IDisposable UseDefaultPipeline()
        {
            var serializer = ApiServiceOptions.PayloadSerializer;
            var compressor = ApiServiceOptions.PayloadCompressor;
            var encryptor = ApiServiceOptions.PayloadEncryptor;

            ApiServiceOptions.Initialize(
                new ApiPayloadOptions { Compressor = "gzip", Encryptor = "aes-cbc-hmac" },
                isDebugMode: true);

            return new Restore(() => ApiServiceOptions.Initialize(serializer, compressor, encryptor));
        }

        private sealed class Restore(Action action) : IDisposable
        {
            public void Dispose() => action();
        }

        [Theory]
        [InlineData(PayloadCodecNames.Json)]
        [InlineData(PayloadCodecNames.MessagePack)]
        [InlineData("")]   // 未宣告 → 相容性常數（MessagePack），回應同樣不宣告
        [DisplayName("回應的 codec 必須與請求宣告的相同，且該 body 真的解得開")]
        public void Execute_EncodedRequest_AnswersWithTheDeclaredCodec(string codec)
        {
            using var _ = UseDefaultPipeline();

            var args = new PingRequest { ClientName = "codec-symmetry", TraceId = "sym-001" };
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.Ping",
                Params = new JsonRpcParams { Codec = codec, Value = args },
                Id = Guid.NewGuid().ToString(),
            };
            ApiPayloadConverter.TransformTo(request.Params, PayloadFormat.Encoded);

            var executor = new JsonRpcExecutor(
                _fx.GetRequiredService<IBusinessObjectFactory>(),
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = Guid.Empty,
                IsLocalCall = true,
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<JsonRpcResult>(response.Result);

            // 宣告面：回應必須自報同一個 codec，否則客戶端無從得知該用什麼解。
            Assert.Equal(codec, result.Codec);

            // 實際面：body 必須真的是那個 codec 寫出來的。RestoreFrom 只讀 payload 自報的
            // codec，所以「宣告對、內容不對」會在這裡炸而不是靜默通過。
            Assert.IsType<byte[]>(result.Value);
            ApiPayloadConverter.RestoreFrom(result, PayloadFormat.Encoded);

            var pong = Assert.IsType<PingResponse>(result.Value);
            Assert.Equal("sym-001", pong.TraceId);
        }
    }
}
