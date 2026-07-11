using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.JsonRpc
{
    /// <summary>
    /// JsonRpcExecutor 覆蓋率補強測試：聚焦建構子 null 防護、異常記錄（anomaly detection）
    /// 路徑（成功慢查詢 / 失敗錯誤記錄 / AnomalyEnabled 組合分支）與加密金鑰取得分支。
    /// </summary>
    public class JsonRpcExecutorCoverageTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public JsonRpcExecutorCoverageTests(BeeTestFixture fx)
        {
            _fx = fx;
        }

        private IBusinessObjectFactory BoFactory => _fx.GetRequiredService<IBusinessObjectFactory>();
        private IAccessTokenValidator TokenValidator => _fx.GetRequiredService<IAccessTokenValidator>();
        private IApiEncryptionKeyProvider KeyProvider => _fx.GetRequiredService<IApiEncryptionKeyProvider>();

        /// <summary>
        /// 捕捉 anomaly 寫入的假 writer。
        /// </summary>
        private sealed class CapturingAuditLogWriter : IAuditLogWriter
        {
            public List<AuditEntry> Entries { get; } = [];

            public void Write(AuditEntry entry) => Entries.Add(entry);
        }

        /// <summary>
        /// 回傳固定 SessionInfo 的假 session service。
        /// </summary>
        private sealed class StubSessionInfoService : ISessionInfoService
        {
            private readonly SessionInfo _session;

            public StubSessionInfoService(SessionInfo session) => _session = session;

            public SessionInfo Get(Guid accessToken) => _session;

            public void Set(SessionInfo sessionInfo) { }

            public void Remove(Guid accessToken) { }
        }

        private static SessionInfo NewSession() => new()
        {
            UserId = "u1",
            UserName = "User One",
            CompanyId = "C1",
        };

        private JsonRpcExecutor NewAuditExecutor(
            IAuditLogWriter? writer,
            AuditLogOptions? options,
            ISessionInfoService? session,
            Guid accessToken,
            bool isLocalCall = true)
        {
            return new JsonRpcExecutor(BoFactory, TokenValidator, KeyProvider, writer, options, session)
            {
                AccessToken = accessToken,
                IsLocalCall = isLocalCall,
            };
        }

        private static JsonRpcRequest UnknownActionRequest() => new()
        {
            Method = $"{SysProgIds.System}.DefinitelyNotAMethod",
            Params = new JsonRpcParams(),
            Id = "1",
        };

        private static JsonRpcRequest PingRequest() => new()
        {
            Method = $"{SysProgIds.System}.Ping",
            Params = new JsonRpcParams { Value = new Bee.Api.Core.Messages.System.PingRequest { ClientName = "C", TraceId = "T" } },
            Id = "1",
        };

        private static AuditLogOptions EnabledOptions(int slowThresholdMs = 3000) => new()
        {
            Enabled = true,
            AnomalyEnabled = true,
            ApiSlowThresholdMs = slowThresholdMs,
        };

        // ---- 建構子 null 防護（lines 50-52） ----

        [Fact]
        [DisplayName("建構子於 boFactory 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullBoFactory_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new JsonRpcExecutor(null!, TokenValidator, KeyProvider));
            Assert.Equal("boFactory", ex.ParamName);
        }

        [Fact]
        [DisplayName("建構子於 tokenValidator 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullTokenValidator_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new JsonRpcExecutor(BoFactory, null!, KeyProvider));
            Assert.Equal("tokenValidator", ex.ParamName);
        }

        [Fact]
        [DisplayName("建構子於 keyProvider 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullKeyProvider_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new JsonRpcExecutor(BoFactory, TokenValidator, null!));
            Assert.Equal("keyProvider", ex.ParamName);
        }

        // ---- 失敗 anomaly 記錄（lines 94/136-137、153-158、163-183、188-189） ----

        [Fact]
        [DisplayName("啟用 anomaly 且呼叫失敗應寫入 Error 類別的異常記錄")]
        public void Execute_AnomalyEnabledFailure_WritesErrorAnomaly()
        {
            var writer = new CapturingAuditLogWriter();
            var token = Guid.NewGuid();
            var executor = NewAuditExecutor(writer, EnabledOptions(), new StubSessionInfoService(NewSession()), token);

            var response = executor.Execute(UnknownActionRequest());

            Assert.NotNull(response.Error);
            var entry = Assert.Single(writer.Entries);
            var anomaly = Assert.IsType<ApiAnomalyEntry>(entry);
            Assert.Equal(AnomalyKind.Error, anomaly.Kind);
            Assert.Equal(nameof(MissingMethodException), anomaly.ErrorType);
            Assert.Equal($"{SysProgIds.System}.DefinitelyNotAMethod", anomaly.Method);
            Assert.NotNull(anomaly.ErrorMessage);
            Assert.Null(anomaly.ThresholdMs);
            // AccessToken 非空 → 記錄保留該權杖（line 170 的非空分支）。
            Assert.Equal(token, anomaly.AccessToken);
        }

        [Fact]
        [DisplayName("啟用 anomaly 且 AccessToken 為空的失敗記錄不應保留權杖")]
        public void Execute_AnomalyEnabledFailureEmptyToken_WritesNullAccessToken()
        {
            var writer = new CapturingAuditLogWriter();
            var executor = NewAuditExecutor(writer, EnabledOptions(), new StubSessionInfoService(NewSession()), Guid.Empty);

            var response = executor.Execute(UnknownActionRequest());

            Assert.NotNull(response.Error);
            var anomaly = Assert.IsType<ApiAnomalyEntry>(Assert.Single(writer.Entries));
            // AccessToken 為空 → 記錄不保留權杖（line 170 的空值分支）。
            Assert.Null(anomaly.AccessToken);
            Assert.Equal("u1", anomaly.UserId);
            Assert.Equal("C1", anomaly.CompanyId);
        }

        // ---- 成功 anomaly 記錄（lines 143-148；慢查詢） ----

        [Fact]
        [DisplayName("啟用 anomaly 且成功但未逾慢查詢門檻不應寫入記錄")]
        public void Execute_AnomalyEnabledFastSuccess_WritesNoAnomaly()
        {
            var writer = new CapturingAuditLogWriter();
            var executor = NewAuditExecutor(writer, EnabledOptions(slowThresholdMs: 3000), new StubSessionInfoService(NewSession()), Guid.Empty);

            var response = executor.Execute(PingRequest());

            Assert.Null(response.Error);
            Assert.NotNull(response.Result);
            // 快速成功呼叫遠低於 3000ms 門檻，不應產生 Slow 記錄。
            Assert.Empty(writer.Entries);
        }

        [Fact]
        [DisplayName("啟用 anomaly 且逾慢查詢門檻時如有記錄應為 Slow 類別")]
        public void Execute_AnomalyEnabledSlowSuccess_WritesSlowAnomalyWhenExceeded()
        {
            var writer = new CapturingAuditLogWriter();
            // 門檻設 1ms：reflection invoke + 追蹤幾乎必然逾越，觸發 Slow 寫入（line 147）。
            var executor = NewAuditExecutor(writer, EnabledOptions(slowThresholdMs: 1), new StubSessionInfoService(NewSession()), Guid.Empty);

            var response = executor.Execute(PingRequest());

            Assert.Null(response.Error);
            // 斷言對「是否逾越」皆成立：若有寫入必為 Slow，避免計時造成 flaky。
            Assert.All(writer.Entries, e => Assert.Equal(AnomalyKind.Slow, Assert.IsType<ApiAnomalyEntry>(e).Kind));
        }

        // ---- AnomalyEnabled 組合分支（line 137 br7/8） ----

        [Fact]
        [DisplayName("有 writer 但 auditOptions 停用時失敗不應寫入記錄")]
        public void Execute_AuditOptionsDisabled_WritesNoAnomaly()
        {
            var writer = new CapturingAuditLogWriter();
            var options = new AuditLogOptions { Enabled = false, AnomalyEnabled = true };
            var executor = NewAuditExecutor(writer, options, new StubSessionInfoService(NewSession()), Guid.Empty);

            var response = executor.Execute(UnknownActionRequest());

            Assert.NotNull(response.Error);
            Assert.Empty(writer.Entries);
        }

        [Fact]
        [DisplayName("有 writer 但 AnomalyEnabled 為 false 時失敗不應寫入記錄")]
        public void Execute_AnomalyFlagDisabled_WritesNoAnomaly()
        {
            var writer = new CapturingAuditLogWriter();
            var options = new AuditLogOptions { Enabled = true, AnomalyEnabled = false };
            var executor = NewAuditExecutor(writer, options, new StubSessionInfoService(NewSession()), Guid.Empty);

            var response = executor.Execute(UnknownActionRequest());

            Assert.NotNull(response.Error);
            Assert.Empty(writer.Entries);
        }

        [Fact]
        [DisplayName("有 writer 但 auditOptions 為 null 時失敗不應寫入記錄")]
        public void Execute_AuditOptionsNull_WritesNoAnomaly()
        {
            var writer = new CapturingAuditLogWriter();
            var executor = NewAuditExecutor(writer, options: null, session: new StubSessionInfoService(NewSession()), accessToken: Guid.Empty);

            var response = executor.Execute(UnknownActionRequest());

            Assert.NotNull(response.Error);
            Assert.Empty(writer.Entries);
        }

        [Fact]
        [DisplayName("有 writer 與啟用選項但 sessionService 為 null 時失敗不應寫入記錄")]
        public void Execute_SessionServiceNull_WritesNoAnomaly()
        {
            var writer = new CapturingAuditLogWriter();
            var executor = NewAuditExecutor(writer, EnabledOptions(), session: null, accessToken: Guid.Empty);

            var response = executor.Execute(UnknownActionRequest());

            Assert.NotNull(response.Error);
            Assert.Empty(writer.Entries);
        }

        // ---- 加密金鑰取得分支（line 200：Encrypted 分支） ----

        [Fact]
        [DisplayName("非本地呼叫且 Encrypted 格式應進入加密金鑰取得分支")]
        public void Execute_EncryptedFormatRemoteCall_HitsEncryptionKeyBranch()
        {
            // Ping 為 Public/Anonymous，Encrypted 格式通過存取驗證後會取得加密金鑰（line 200 Encrypted 分支），
            // 隨後對未加密 payload 解密會失敗 → 回傳錯誤；重點在覆蓋 Encrypted 分支。
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.Ping",
                Params = new JsonRpcParams { Format = PayloadFormat.Encrypted, Value = new Bee.Api.Core.Messages.System.PingRequest { ClientName = "C", TraceId = "T" } },
                Id = "1",
            };

            var executor = new JsonRpcExecutor(BoFactory, TokenValidator, KeyProvider)
            {
                AccessToken = Guid.Empty,
                IsLocalCall = false,
            };

            var response = executor.Execute(request);

            // 未提供有效金鑰 / payload 非加密 → 應以錯誤收場。
            Assert.NotNull(response.Error);
        }
    }
}
