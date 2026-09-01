using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.System;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 重放防護 frame 走完整 payload 管線的行為測試。
    /// </summary>
    /// <remarks>
    /// 會改寫 <see cref="ApiServiceOptions.RequireWireFrame"/> 這個 process-wide 靜態開關，
    /// 故掛 ApiServiceOptionsState collection 標記，並一律以 try/finally 還原。
    /// </remarks>
    [Collection("ApiServiceOptionsState")]
    public class WireFrameReplayTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public WireFrameReplayTests(BeeTestFixture fx)
        {
            _fx = fx;
        }

        private static byte[] MakeKey()
        {
            var key = new byte[64];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)i;
            return key;
        }

        private static void WithFrameRequired(bool value, Action action)
        {
            bool original = ApiServiceOptions.RequireWireFrame;
            ApiServiceOptions.RequireWireFrame = value;
            try { action(); }
            finally { ApiServiceOptions.RequireWireFrame = original; }
        }

        [Fact]
        [DisplayName("開關關閉時 Encrypted round-trip 不應產生 frame")]
        public void RestoreFrom_FrameNotRequired_LeavesFrameNull()
        {
            WithFrameRequired(false, () =>
            {
                var key = MakeKey();
                var payload = new JsonRpcParams { Value = new PingRequest { ClientName = "a" } };

                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encrypted, key);
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encrypted, key);

                Assert.Null(payload.Frame);
                Assert.IsType<PingRequest>(payload.Value);
            });
        }

        [Fact]
        [DisplayName("開關開啟時 Encrypted round-trip 應還原 frame 與 body")]
        public void RestoreFrom_FrameRequired_RoundTripsFrameAndBody()
        {
            WithFrameRequired(true, () =>
            {
                var key = MakeKey();
                var payload = new JsonRpcParams { Value = new PingRequest { ClientName = "a" } };

                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encrypted, key);
                ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encrypted, key);

                Assert.NotNull(payload.Frame);
                Assert.Equal(ApiPayloadFrame.CurrentVersion, payload.Frame!.Version);
                Assert.Equal("a", Assert.IsType<PingRequest>(payload.Value).ClientName);
            });
        }

        [Fact]
        [DisplayName("Plain 格式即使開關開啟也不帶 frame")]
        public void TransformTo_PlainWithFrameRequired_WritesNoFrame()
        {
            // Plain 沒有封套，frame 放進去也只是明文、攻擊者可任意改寫，等於沒防護。
            WithFrameRequired(true, () =>
            {
                var payload = new JsonRpcParams { Value = "hello" };

                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Plain);

                Assert.Null(payload.Frame);
                Assert.Equal("hello", payload.Value);
            });
        }

        [Fact]
        [DisplayName("寫入端有 frame 而讀取端未預期時應解碼失敗（兩端設定必須一致）")]
        public void RestoreFrom_FrameWrittenButNotExpected_FailsToDecode()
        {
            // frame 的有無不由封包自述（那會是降級攻擊面），因此兩端設定不一致時
            // 必然失敗——這正是預期行為，也是升級時要先兩端佈署再開開關的理由。
            var key = MakeKey();
            var payload = new JsonRpcParams { Value = new PingRequest { ClientName = "a" } };

            bool original = ApiServiceOptions.RequireWireFrame;
            try
            {
                ApiServiceOptions.RequireWireFrame = true;
                ApiPayloadConverter.TransformTo(payload, PayloadFormat.Encrypted, key);

                ApiServiceOptions.RequireWireFrame = false;
                Assert.Throws<InvalidOperationException>(() =>
                    ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encrypted, key));
            }
            finally
            {
                ApiServiceOptions.RequireWireFrame = original;
            }
        }

        [Fact]
        [DisplayName("frame 時間戳超出容許時窗應回 ReplayRejected")]
        public void Execute_FrameTimestampOutsideWindow_ReturnsReplayRejected()
        {
            WithFrameRequired(true, () =>
            {
                var staleMs = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();

                var response = ExecutePing(new ApiPayloadFrame(staleMs, sequence: 0));

                Assert.NotNull(response.Error);
                Assert.Equal((int)JsonRpcErrorCode.ReplayRejected, response.Error!.Code);
            });
        }

        [Fact]
        [DisplayName("frame 時間戳落在容許時窗內應正常執行")]
        public void Execute_FrameTimestampWithinWindow_Succeeds()
        {
            WithFrameRequired(true, () =>
            {
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var response = ExecutePing(new ApiPayloadFrame(nowMs, sequence: 0));

                Assert.Null(response.Error);
            });
        }

        /// <summary>
        /// 以指定的 frame 送出一次 Encoded 的 Ping 呼叫。
        /// </summary>
        /// <param name="frame">要夾帶的重放防護 frame。</param>
        private JsonRpcResponse ExecutePing(ApiPayloadFrame frame)
        {
            var executor = new JsonRpcExecutor(
                _fx.GetRequiredService<IBusinessObjectFactory>(),
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = Guid.Empty,
                IsLocalCall = true,
            };

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.Ping",
                Params = new JsonRpcParams
                {
                    Value = new PingRequest { ClientName = "replay-test" },
                    Frame = frame,
                },
                Id = Guid.NewGuid().ToString(),
            };

            // Encoded 而非 Encrypted：時窗檢查讀的是 frame，與加密無關，而 Encoded 不需要
            // 傳輸金鑰，測試因此不必先建立 session（那會碰資料庫）。
            ApiPayloadConverter.TransformTo(request.Params, PayloadFormat.Encoded);

            return executor.Execute(request);
        }
    }
}
