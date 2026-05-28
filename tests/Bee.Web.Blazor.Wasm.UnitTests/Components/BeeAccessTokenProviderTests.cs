using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Wasm.Components;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// Structural and behavioural checks for <see cref="BeeAccessTokenProvider"/>.
    /// State mutations are exercised directly (no renderer attached); cascading
    /// propagation is covered indirectly by <see cref="FormPage"/> declaring a
    /// matching <c>[CascadingParameter] public Guid AccessToken</c>.
    /// </summary>
    public class BeeAccessTokenProviderTests
    {
        [Fact]
        [DisplayName("BeeAccessTokenProvider 為 Blazor ComponentBase 子類別")]
        public void Type_IsComponentBaseSubclass()
        {
            Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(BeeAccessTokenProvider)));
        }

        [Fact]
        [DisplayName("ChildContent 為 RenderFragment<BeeAccessTokenProvider> 並標註 [Parameter]")]
        public void ChildContent_IsTemplatedParameter()
        {
            var property = typeof(BeeAccessTokenProvider).GetProperty(
                nameof(BeeAccessTokenProvider.ChildContent),
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.Equal(typeof(RenderFragment<BeeAccessTokenProvider>), property!.PropertyType);
            Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());
        }

        [Fact]
        [DisplayName("初始狀態 AccessToken 為 Guid.Empty 且 IsAuthenticated 為 false")]
        public void InitialState_IsAnonymous()
        {
            var provider = new BeeAccessTokenProvider();
            Assert.Equal(Guid.Empty, provider.AccessToken);
            Assert.False(provider.IsAuthenticated);
        }

        [Fact]
        [DisplayName("SetToken 指派非空 Guid 後 AccessToken 與 IsAuthenticated 同步更新")]
        public void SetToken_NonEmptyGuid_UpdatesStateAndAuthenticatedFlag()
        {
            var provider = new BeeAccessTokenProvider();
            var token = Guid.NewGuid();

            provider.SetToken(token);

            Assert.Equal(token, provider.AccessToken);
            Assert.True(provider.IsAuthenticated);
        }

        [Fact]
        [DisplayName("Clear 將 AccessToken 重設為 Guid.Empty 且 IsAuthenticated 變回 false")]
        public void Clear_ResetsStateToAnonymous()
        {
            var provider = new BeeAccessTokenProvider();
            provider.SetToken(Guid.NewGuid());

            provider.Clear();

            Assert.Equal(Guid.Empty, provider.AccessToken);
            Assert.False(provider.IsAuthenticated);
        }

        [Fact]
        [DisplayName("SetToken 對同值不應拋出例外（無 renderer 仍安全）")]
        public void SetToken_IdempotentForSameValue_DoesNotThrow()
        {
            var provider = new BeeAccessTokenProvider();
            var token = Guid.NewGuid();
            provider.SetToken(token);

            var exception = Record.Exception(() => provider.SetToken(token));

            Assert.Null(exception);
            Assert.Equal(token, provider.AccessToken);
        }

        [Fact]
        [DisplayName("OnInitialized 呼叫後 _isAttached 應設為 true")]
        public void OnInitialized_SetsIsAttachedToTrue()
        {
            var provider = new BeeAccessTokenProvider();
            var isAttachedField = typeof(BeeAccessTokenProvider).GetField(
                "_isAttached", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(isAttachedField);
            Assert.False((bool)isAttachedField!.GetValue(provider)!);
            var method = typeof(BeeAccessTokenProvider).GetMethod(
                "OnInitialized", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(provider, null);
            Assert.True((bool)isAttachedField.GetValue(provider)!);
        }
    }
}
