using System.ComponentModel;
using System.Reflection;
using Bee.Api.Core.Messages.System;
using Bee.Web.Blazor.Wasm.Components;
using Bee.Web.Blazor.Wasm.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// Structural smoke tests for <see cref="BeeLoginPanel"/>: confirms the
    /// public parameter surface, the <see cref="BeeApiConnectorFactory"/>
    /// injection, and the labels' defaults. Submitting against a real backend
    /// is exercised by the Phase 2 sample (BlazorHostApp), not by an in-memory
    /// unit test.
    /// </summary>
    public class BeeLoginPanelTests
    {
        private static PropertyInfo GetProperty(string name)
        {
            var property = typeof(BeeLoginPanel).GetProperty(
                name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            return property!;
        }

        private static PropertyInfo GetNonPublicProperty(string name)
        {
            var property = typeof(BeeLoginPanel).GetProperty(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(property);
            return property!;
        }

        [Fact]
        [DisplayName("BeeLoginPanel 為 Blazor ComponentBase 子類別")]
        public void Type_IsComponentBaseSubclass()
        {
            Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(BeeLoginPanel)));
        }

        [Theory]
        [InlineData(nameof(BeeLoginPanel.UserIdLabel))]
        [InlineData(nameof(BeeLoginPanel.PasswordLabel))]
        [InlineData(nameof(BeeLoginPanel.SubmitLabel))]
        [InlineData(nameof(BeeLoginPanel.OnLoggedIn))]
        [DisplayName("公開屬性皆標註 [Parameter]")]
        public void PublicProperties_AreMarkedAsParameters(string name)
        {
            var property = GetProperty(name);
            Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());
        }

        [Fact]
        [DisplayName("OnLoggedIn 屬性型別為 EventCallback<LoginResponse>")]
        public void OnLoggedIn_IsEventCallbackOfLoginResponse()
        {
            var property = GetProperty(nameof(BeeLoginPanel.OnLoggedIn));
            Assert.Equal(typeof(EventCallback<LoginResponse>), property.PropertyType);
        }

        [Fact]
        [DisplayName("Factory 屬性透過 [Inject] 注入 BeeApiConnectorFactory")]
        public void Factory_IsInjected()
        {
            var property = GetNonPublicProperty("Factory");
            Assert.NotNull(property.GetCustomAttribute<InjectAttribute>());
            Assert.Equal(typeof(BeeApiConnectorFactory), property.PropertyType);
        }

        [Fact]
        [DisplayName("Label 屬性具有預設值 User ID / Password / Sign in")]
        public void LabelProperties_HaveSensibleDefaults()
        {
            var panel = new BeeLoginPanel();
            Assert.Equal("User ID", panel.UserIdLabel);
            Assert.Equal("Password", panel.PasswordLabel);
            Assert.Equal("Sign in", panel.SubmitLabel);
        }
    }
}
