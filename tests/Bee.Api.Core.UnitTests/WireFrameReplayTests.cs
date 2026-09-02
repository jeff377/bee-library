using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.System;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 重放防護 frame 走完整 payload 管線的行為測試。
    /// </summary>
    /// <remarks>
    /// 會改寫 <see cref="ApiServiceOptions.RequireWireFrame"/> 這個 process-wide 靜態開關，
    /// 故掛 ApiServiceOptionsState collection 標記，並一律以 try/finally 還原。
    /// <para>
    /// fixture 必須是 <see cref="SharedDbFixture"/>（會建 schema）而非 <see cref="BeeTestFixture"/>
    /// （不建）：本類別以 <c>Guid.NewGuid()</c> 當 access token，未植入 session 快取，server 端
    /// 因此走 rebuild 路徑讀 <c>st_session</c>。掛錯 fixture 只有在「別的類別或行程剛好先把表建好」
    /// 時才會過 —— 對著全新的資料庫就會以 <c>Invalid object name 'st_session'</c> 現形。
    /// </para>
    /// </remarks>
    [Collection("ApiServiceOptionsState")]
    public class WireFrameReplayTests : IClassFixture<SharedDbFixture>
    {
        private readonly BeeTestFixture _fx;

        public WireFrameReplayTests(SharedDbFixture fx)
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

        [Fact]
        [DisplayName("宣告 UniqueSequence 的方法重複序號應回 ReplayRejected")]
        public void Execute_RepeatedSequenceOnGuardedMethod_ReturnsReplayRejected()
        {
            WithFrameRequired(true, () =>
            {
                // 每個測試用獨立 token，視窗才不會與其他測試互相干擾。
                var token = Guid.NewGuid();

                var first = Execute("ExecFunc", new ExecFuncRequest("noop"), FrameWith(1), token);
                var replay = Execute("ExecFunc", new ExecFuncRequest("noop"), FrameWith(1), token);
                var nextSequence = Execute("ExecFunc", new ExecFuncRequest("noop"), FrameWith(2), token);

                // 第一次會一路走到 BO 內部（"noop" 這個自訂方法不存在，故為 InternalError）
                // ——重點在它不是 ReplayRejected，表示序號檢查放行了。
                Assert.Equal((int)JsonRpcErrorCode.InternalError, first.Error!.Code);
                Assert.Equal((int)JsonRpcErrorCode.ReplayRejected, replay.Error!.Code);
                // 換一個序號仍應放行，證明拒絕是針對重複而非一律擋下。
                Assert.Equal((int)JsonRpcErrorCode.InternalError, nextSequence.Error!.Code);
            });
        }

        [Fact]
        [DisplayName("未宣告序號檢查的方法重複序號應正常執行")]
        public void Execute_RepeatedSequenceOnUnguardedMethod_Succeeds()
        {
            // 查詢類方法重放無害，全面套用只是徒增每次呼叫的判斷。
            WithFrameRequired(true, () =>
            {
                var token = Guid.NewGuid();
                var value = new PingRequest { ClientName = "replay-test" };

                Assert.Null(Execute("Ping", value, FrameWith(1), token).Error);
                Assert.Null(Execute("Ping", value, FrameWith(1), token).Error);
            });
        }

        [Fact]
        [DisplayName("匿名呼叫不做序號檢查（無 session 可計數）")]
        public void Execute_RepeatedSequenceAnonymously_Succeeds()
        {
            // 序號是 per session 的。匿名呼叫全共用 Guid.Empty，若也檢查，
            // 不同用戶端會互相把對方的序號用掉而大量誤拒。
            WithFrameRequired(true, () =>
            {
                var first = Execute("ExecFunc", new ExecFuncRequest("noop"), FrameWith(1), Guid.Empty);
                var replay = Execute("ExecFunc", new ExecFuncRequest("noop"), FrameWith(1), Guid.Empty);

                // 同上，InternalError 表示兩次都通過了序號閘門走到 BO 內部。
                Assert.Equal((int)JsonRpcErrorCode.InternalError, first.Error!.Code);
                Assert.Equal((int)JsonRpcErrorCode.InternalError, replay.Error!.Code);
            });
        }

        [Fact]
        [DisplayName("重放拒絕應記為 AnomalyKind.Replay 而非 Error")]
        public void Execute_ReplayRejected_IsLoggedAsReplayAnomaly()
        {
            // 折進泛用 Error 的話，「某 session 連續被拒」這個訊號就看不見了——
            // 而那正好是用戶端時鐘偏移或有人重送封包的判別依據。
            WithFrameRequired(true, () =>
            {
                var staleMs = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();

                var entries = ExecuteAndCaptureAnomalies(new ApiPayloadFrame(staleMs, sequence: 0));

                var entry = Assert.IsType<ApiAnomalyEntry>(Assert.Single(entries));
                Assert.Equal(AnomalyKind.Replay, entry.Kind);
            });
        }

        /// <summary>
        /// 以指定的 frame 送出一次 Encoded 的 Ping 呼叫（Ping 未宣告序號檢查）。
        /// </summary>
        /// <param name="frame">要夾帶的重放防護 frame。</param>
        private JsonRpcResponse ExecutePing(ApiPayloadFrame frame)
            => Execute("Ping", new PingRequest { ClientName = "replay-test" }, frame, Guid.Empty);

        /// <summary>
        /// 以指定的 frame 與 token 送出一次 Encoded 的 SystemBO 呼叫。
        /// </summary>
        /// <param name="action">動作名稱。</param>
        /// <param name="value">傳入值。</param>
        /// <param name="frame">要夾帶的重放防護 frame。</param>
        /// <param name="accessToken">存取權杖；<see cref="Guid.Empty"/> 代表匿名呼叫。</param>
        private JsonRpcResponse Execute(string action, object value, ApiPayloadFrame frame, Guid accessToken)
        {
            var executor = new JsonRpcExecutor(
                _fx.GetRequiredService<IBusinessObjectFactory>(),
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                // 本機呼叫可跳過 token 驗證，讓測試不必先建立 session（那會碰資料庫）；
                // 序號檢查本身與此無關，它只看 token 是否為 Empty。
                IsLocalCall = true,
            };

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.{action}",
                Params = new JsonRpcParams { Value = value, Frame = frame },
                Id = Guid.NewGuid().ToString(),
            };

            // Encoded 而非 Encrypted：frame 的讀取與加密無關，而 Encoded 不需要傳輸金鑰。
            ApiPayloadConverter.TransformTo(request.Params, PayloadFormat.Encoded);

            return executor.Execute(request);
        }

        private static ApiPayloadFrame FrameWith(long sequence)
            => new(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sequence);

        /// <summary>
        /// 以啟用 anomaly 記錄的 executor 送出一次呼叫，回傳捕捉到的 anomaly 紀錄。
        /// </summary>
        /// <param name="frame">要夾帶的重放防護 frame。</param>
        private List<AnomalyEntry> ExecuteAndCaptureAnomalies(ApiPayloadFrame frame)
        {
            var writer = new CapturingAnomalyLogWriter();
            var executor = new JsonRpcExecutor(
                _fx.GetRequiredService<IBusinessObjectFactory>(),
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>(),
                writer,
                new AuditLogOptions { Enabled = true, AnomalyEnabled = true, ApiSlowThresholdMs = 60_000 },
                new StubSessionInfoService())
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
            ApiPayloadConverter.TransformTo(request.Params, PayloadFormat.Encoded);
            executor.Execute(request);

            return writer.Entries;
        }

        private sealed class CapturingAnomalyLogWriter : IAnomalyLogWriter
        {
            public List<AnomalyEntry> Entries { get; } = [];

            public void Write(AnomalyEntry entry) => Entries.Add(entry);
        }

        private sealed class StubSessionInfoService : ISessionInfoService
        {
            public SessionInfo Get(Guid accessToken) => new() { UserId = "u1", UserName = "User One" };

            public void Set(SessionInfo sessionInfo) { }

            public void Remove(Guid accessToken) { }
        }
    }
}
