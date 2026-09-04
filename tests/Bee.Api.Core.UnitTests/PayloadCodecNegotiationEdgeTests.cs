using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.System;
using Bee.Api.Core.Transformers;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// codec 協商的兩個邊角：顯式指名部署預設、以及可用清單要與實際接受的一致。
    /// </summary>
    [Collection("ApiServiceOptionsState")]
    public class PayloadCodecNegotiationEdgeTests
    {
        /// <summary>
        /// 只實作兩參數多載的 transformer —— 也就是協商機制出現之前的每一個自訂 transformer。
        /// </summary>
        private sealed class LegacyTransformer : IApiPayloadTransformer
        {
            private readonly IApiPayloadTransformer _inner = ApiServiceOptions.PayloadTransformer;

            public byte[] Encode(object payload, Type type) => _inner.Encode(payload, type);

            public object? Decode(object payload, Type type) => _inner.Decode(payload, type);

            public byte[] Encrypt(byte[] rawBytes, byte[] encryptionKey) => _inner.Encrypt(rawBytes, encryptionKey);

            public byte[] Decrypt(byte[] encryptedBytes, byte[] encryptionKey) => _inner.Decrypt(encryptedBytes, encryptionKey);
        }

        [Fact]
        [DisplayName("顯式指名部署預設的 codec，不得要求 transformer 支援協商")]
        public void Encode_CodecNamesTheDeploymentDefault_UsesTheTwoArgumentOverload()
        {
            // 這是最自然的寫法：client 想確定用哪個 codec 就把它寫出來。它什麼都沒改變，
            // 卻曾經讓只實作兩參數多載的自訂 transformer 吃到 NotSupportedException。
            var previousTransformer = ApiServiceOptions.PayloadTransformer;
            try
            {
                ApiServiceOptions.PayloadTransformer = new LegacyTransformer();
                var payload = new JsonRpcParams
                {
                    Value = new PingRequest { ClientName = "a" },
                    Codec = ApiServiceOptions.PayloadSerializer.SerializationMethod,
                };

                var exception = Record.Exception(
                    () => ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encoded));

                Assert.Null(exception);

                // 對照組：指名一個「會改變 body」的 codec 才該要求新能力。
                var other = ApiServiceOptions.AcceptedPayloadCodecs
                    .First(c => !string.Equals(c, ApiServiceOptions.PayloadSerializer.SerializationMethod, StringComparison.Ordinal));
                var negotiated = new JsonRpcParams { Value = new PingRequest { ClientName = "a" }, Codec = other };

                Assert.Throws<NotSupportedException>(
                    () => ApiPayloadConverter.TransformTo(negotiated, PayloadFormat.Encoded));
            }
            finally
            {
                ApiServiceOptions.PayloadTransformer = previousTransformer;
            }
        }

        [Fact]
        [DisplayName("AcceptedPayloadCodecs 必須涵蓋 ResolvePayloadSerializer 實際接受的每個名稱")]
        public void AcceptedPayloadCodecs_CoversEveryNameThatResolves()
        {
            // 這份清單就是 client 拿來協商的依據。裝了自訂 serializer 的部署曾經被告知
            // 「你接受的那個 codec 不存在」。
            foreach (var codec in ApiServiceOptions.AcceptedPayloadCodecs)
            {
                var serializer = ApiServiceOptions.ResolvePayloadSerializer(codec);
                Assert.NotNull(serializer);
            }

            Assert.Contains(
                ApiServiceOptions.PayloadSerializer.SerializationMethod,
                ApiServiceOptions.AcceptedPayloadCodecs,
                StringComparer.Ordinal);
        }
    }
}
