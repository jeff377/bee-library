using System.ComponentModel;
using Bee.Business.Form;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition.Identity;
using Bee.Definition.Language;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessObjectFactory"/> 的 BO 型別解析客製化接線測試：
    /// 應以 <c>SessionInfo.CustomizeId</c> 呼叫 <see cref="IBoTypeResolver.Resolve(string, string)"/>；
    /// 無 session / CustomizeId 為空時傳空字串（解析結果逐位元同現況）。
    /// </summary>
    public class BusinessObjectFactoryCustomizeTests
    {
        [Fact]
        [DisplayName("CreateBusinessObject 應以 session 的 CustomizeId 解析 BO 型別")]
        public void CreateBusinessObject_Form_PassesSessionCustomizeIdToResolver()
        {
            var resolver = new SpyResolver();
            var token = Guid.NewGuid();
            var sessions = new StubSessionInfoService();
            sessions.Add(new SessionInfo { AccessToken = token, CustomizeId = "acme" });
            var factory = CreateFactory(resolver, sessions);

            factory.CreateBusinessObject(token, "P001");

            Assert.Equal("acme", resolver.LastCustomizeId);
            Assert.Equal("P001", resolver.LastProgId);
        }

        [Fact]
        [DisplayName("解析出的客製 BO 型別應被實際建立")]
        public void CreateBusinessObject_Form_InstantiatesResolvedType()
        {
            var resolver = new SpyResolver { ResolvedType = typeof(TenantFormBo) };
            var token = Guid.NewGuid();
            var sessions = new StubSessionInfoService();
            sessions.Add(new SessionInfo { AccessToken = token, CustomizeId = "acme" });
            var factory = CreateFactory(resolver, sessions);

            var bo = factory.CreateBusinessObject(token, "P001");

            Assert.IsType<TenantFormBo>(bo);
        }

        // ---- 回歸防護：未設 CustomizeId 的部署行為必須與現況逐位元一致 ----

        [Fact]
        [DisplayName("回歸防護：session 未設 CustomizeId 時應以空字串解析（純 base）")]
        public void CreateBusinessObject_Form_SessionWithoutCustomizeId_PassesEmpty()
        {
            var resolver = new SpyResolver();
            var token = Guid.NewGuid();
            var sessions = new StubSessionInfoService();
            sessions.Add(new SessionInfo { AccessToken = token });
            var factory = CreateFactory(resolver, sessions);

            factory.CreateBusinessObject(token, "P001");

            Assert.Equal(string.Empty, resolver.LastCustomizeId);
        }

        [Fact]
        [DisplayName("回歸防護：AccessToken 為空時不查 session，以空字串解析")]
        public void CreateBusinessObject_Form_EmptyAccessToken_SkipsSessionLookup()
        {
            var resolver = new SpyResolver();
            var sessions = new StubSessionInfoService();
            var factory = CreateFactory(resolver, sessions);

            factory.CreateBusinessObject(Guid.Empty, "P001");

            Assert.Equal(string.Empty, resolver.LastCustomizeId);
            Assert.Equal(0, sessions.GetCallCount);
        }

        [Fact]
        [DisplayName("回歸防護：session 不存在時以空字串解析，不拋例外")]
        public void CreateBusinessObject_Form_NoSession_PassesEmpty()
        {
            var resolver = new SpyResolver();
            var sessions = new StubSessionInfoService(); // 未註冊任何 session
            var factory = CreateFactory(resolver, sessions);

            var exception = Record.Exception(() => factory.CreateBusinessObject(Guid.NewGuid(), "P001"));

            Assert.Null(exception);
            Assert.Equal(string.Empty, resolver.LastCustomizeId);
        }

        // ---- Fixtures ----

        public class TenantFormBo : FormBusinessObject
        {
            public TenantFormBo(Definition.IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
                : base(ctx, accessToken, progId, isLocalCall) { }
        }

        // DefineAccess / LanguageService are only forwarded onto the BeeContext and never touched by
        // the resolution path under test, but the factory ctor null-checks every dependency, so
        // inert stubs stand in for them.
        private static BusinessObjectFactory CreateFactory(IBoTypeResolver resolver, ISessionInfoService sessions)
            => new BusinessObjectFactory(
                new EmptyServiceProvider(), new FakeDefineAccess(), sessions, new InertLanguageService(), resolver);

        // ---- Test doubles ----

        private sealed class SpyResolver : IBoTypeResolver
        {
            public Type ResolvedType { get; set; } = typeof(FormBusinessObject);
            public string? LastCustomizeId { get; private set; }
            public string? LastProgId { get; private set; }

            public Type Resolve(string progId) => Resolve(string.Empty, progId);

            public Type Resolve(string customizeId, string progId)
            {
                LastCustomizeId = customizeId;
                LastProgId = progId;
                return ResolvedType;
            }
        }

        private sealed class StubSessionInfoService : ISessionInfoService
        {
            private readonly Dictionary<Guid, SessionInfo> _sessions = [];

            public int GetCallCount { get; private set; }

            public void Add(SessionInfo sessionInfo) => _sessions[sessionInfo.AccessToken] = sessionInfo;

            public SessionInfo Get(Guid accessToken)
            {
                GetCallCount++;
                return _sessions.TryGetValue(accessToken, out var s) ? s : null!;
            }

            public void Set(SessionInfo sessionInfo) => Add(sessionInfo);

            public void Remove(Guid accessToken) => _sessions.Remove(accessToken);
        }

        private sealed class EmptyServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }

        /// <summary>
        /// Satisfies the factory's null check without participating in any test — the resolution
        /// path never resolves language text.
        /// </summary>
        private sealed class InertLanguageService : ILanguageService
        {
            public string GetLangText(string lang, string fullKey) => throw new NotSupportedException();
            public string GetLangText(string lang, string @namespace, string subKey) => throw new NotSupportedException();
            public bool TryGetLangText(string lang, string fullKey, out string text) => throw new NotSupportedException();
            public bool TryGetLangText(string lang, string @namespace, string subKey, out string text) => throw new NotSupportedException();
            public LanguageEnum? GetLangEnum(string lang, string fullName) => throw new NotSupportedException();
            public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName) => throw new NotSupportedException();
            public string? GetLangEnumText(string lang, string fullName, string code) => throw new NotSupportedException();
        }
    }
}
