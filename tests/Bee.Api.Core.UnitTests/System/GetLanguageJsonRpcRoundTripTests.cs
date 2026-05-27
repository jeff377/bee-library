using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.System;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests.System
{
    /// <summary>
    /// 走 <see cref="JsonRpcExecutor"/> 的 end-to-end round-trip：將
    /// <c>System.GetLanguage</c> 透過 executor 派發到
    /// <see cref="Bee.Business.System.SystemBusinessObject.GetLanguage"/>，驗證：
    /// <list type="bullet">
    /// <item>action 路由（progId.action 反射查表）正確找到方法</item>
    /// <item>ApiInputConverter（GetLanguageRequest → GetLanguageArgs）保留 Lang / Namespace</item>
    /// <item>ApiOutputConverter（GetLanguageResult → GetLanguageResponse）命名慣例反射有作用，
    ///   LanguageResource 物件 deep-copy 正確</item>
    /// <item>從 IDefineAccess 取出 fixture seed 的語系資源</item>
    /// </list>
    /// </summary>
    public class GetLanguageJsonRpcRoundTripTests : IClassFixture<GetLanguageJsonRpcRoundTripTests.LangFixture>
    {
        private readonly LangFixture _fx;

        public GetLanguageJsonRpcRoundTripTests(LangFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("System.GetLanguage 經 JsonRpcExecutor 應派發成功並回傳 fixture seed 的 LanguageResource")]
        public void GetLanguage_ThroughJsonRpc_DispatchesAndReturnsResource()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);

            var boFactory = new BusinessObjectFactory(
                _fx.Provider,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<ILanguageService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            var executor = new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                IsLocalCall = true,
            };

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.{SystemActions.GetLanguage}",
                Params = new JsonRpcParams
                {
                    Value = new GetLanguageRequest
                    {
                        Lang = LangFixture.SeedLang,
                        Namespace = LangFixture.SeedNamespace,
                    },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetLanguageResponse>(response.Result!.Value);
            Assert.NotNull(result.Resource);
            Assert.Equal(LangFixture.SeedNamespace, result.Resource!.Namespace);
            Assert.Equal(LangFixture.SeedLang, result.Resource.Lang);
            Assert.Equal("你好", result.Resource.GetText("Greeting"));
            var gender = result.Resource.GetEnum("Gender");
            Assert.NotNull(gender);
            Assert.Equal("男", gender!.GetText("M"));
        }

        [Fact]
        [DisplayName("System.GetLanguage 對不存在的 namespace 應 dispatch 成功並回 Resource = null")]
        public void GetLanguage_MissingNamespace_DispatchSucceedsWithNullResource()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);

            var boFactory = new BusinessObjectFactory(
                _fx.Provider,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<ILanguageService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            var executor = new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                IsLocalCall = true,
            };

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.{SystemActions.GetLanguage}",
                Params = new JsonRpcParams
                {
                    Value = new GetLanguageRequest { Lang = "zh-TW", Namespace = "Nonexistent" },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.Null(response.Error);
            var result = Assert.IsType<GetLanguageResponse>(response.Result!.Value);
            Assert.Null(result.Resource);
        }

        [Fact]
        [DisplayName("System.GetLanguage 對空 Lang 應回 RpcError")]
        public void GetLanguage_EmptyLang_ReturnsRpcError()
        {
            var accessToken = TestSessionFactory.CreateAccessToken(_fx);

            var boFactory = new BusinessObjectFactory(
                _fx.Provider,
                _fx.GetRequiredService<IDefineAccess>(),
                _fx.GetRequiredService<ISessionInfoService>(),
                _fx.GetRequiredService<ILanguageService>(),
                _fx.GetRequiredService<IFormBoTypeResolver>());

            var executor = new JsonRpcExecutor(
                boFactory,
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                IsLocalCall = true,
            };

            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.{SystemActions.GetLanguage}",
                Params = new JsonRpcParams
                {
                    Value = new GetLanguageRequest { Lang = "", Namespace = "Common" },
                },
                Id = Guid.NewGuid().ToString(),
            };

            var response = executor.Execute(request);

            Assert.NotNull(response.Error);
            Assert.Contains("Lang is required", response.Error!.Message);
        }

        /// <summary>
        /// Writable fixture: copies the shared <c>tests/Define</c> to a temp dir, then
        /// seeds a <see cref="LanguageResource"/> via <see cref="IDefineAccess.SaveLanguage"/>
        /// for the dispatch test to read back through the JSON-RPC pipeline.
        /// </summary>
        public sealed class LangFixture : BeeTestFixture
        {
            public const string SeedLang = "zh-TW";
            public const string SeedNamespace = "TestSys";

            public LangFixture() : base(b => b.UseTempDefinePath())
            {
                var defineAccess = GetRequiredService<IDefineAccess>();
                var resource = new LanguageResource
                {
                    Namespace = SeedNamespace,
                    Lang = SeedLang,
                };
                resource.Items.Add("Greeting", "你好");
                var gender = new LanguageEnum { Name = "Gender" };
                gender.Entries.Add("M", "男");
                gender.Entries.Add("F", "女");
                resource.Enums.Add(gender);
                defineAccess.SaveLanguage(resource);
            }
        }
    }
}
