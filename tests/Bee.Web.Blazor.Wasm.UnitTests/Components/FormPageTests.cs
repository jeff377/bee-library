using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Wasm.Components;
using Bee.Web.Blazor.Wasm.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// Structural smoke tests for <see cref="FormPage"/>: confirms the component
    /// type hierarchy, the public/private parameter surface, and default values.
    /// Actual rendering behaviour (OnInitializedAsync, data loading) requires a
    /// running backend and is exercised by the host sample application, not by
    /// in-memory unit tests.
    /// </summary>
    public class FormPageTests
    {
        private static PropertyInfo GetPublicProperty(string name)
        {
            var property = typeof(FormPage).GetProperty(
                name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
            return property!;
        }

        private static PropertyInfo GetNonPublicProperty(string name)
        {
            var property = typeof(FormPage).GetProperty(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(property);
            return property!;
        }

        [Fact]
        [DisplayName("FormPage 為 Blazor ComponentBase 子類別")]
        public void Type_IsComponentBaseSubclass()
        {
            Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(FormPage)));
        }

        [Fact]
        [DisplayName("ProgId 屬性標註 [Parameter]")]
        public void ProgId_IsMarkedAsParameter()
        {
            var property = GetPublicProperty(nameof(FormPage.ProgId));
            Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());
        }

        [Fact]
        [DisplayName("ProgId 屬性標註 [EditorRequired]")]
        public void ProgId_IsMarkedAsEditorRequired()
        {
            var property = GetPublicProperty(nameof(FormPage.ProgId));
            Assert.NotNull(property.GetCustomAttribute<EditorRequiredAttribute>());
        }

        [Fact]
        [DisplayName("AccessToken 屬性標註 [CascadingParameter]")]
        public void AccessToken_IsMarkedAsCascadingParameter()
        {
            var property = GetPublicProperty(nameof(FormPage.AccessToken));
            Assert.NotNull(property.GetCustomAttribute<CascadingParameterAttribute>());
        }

        [Fact]
        [DisplayName("Factory 私有屬性標註 [Inject] 且型別為 BeeApiConnectorFactory")]
        public void Factory_IsInjectedBeeApiConnectorFactory()
        {
            var property = GetNonPublicProperty("Factory");
            Assert.NotNull(property.GetCustomAttribute<InjectAttribute>());
            Assert.Equal(typeof(BeeApiConnectorFactory), property.PropertyType);
        }

        [Fact]
        [DisplayName("可建立 FormPage 執行個體且 ProgId 預設為空字串")]
        public void Instance_ProgIdDefaultsToEmptyString()
        {
            var page = new FormPage();
            Assert.Equal(string.Empty, page.ProgId);
        }

        [Fact]
        [DisplayName("可建立 FormPage 執行個體且 AccessToken 預設為 Guid.Empty")]
        public void Instance_AccessTokenDefaultsToGuidEmpty()
        {
            var page = new FormPage();
            Assert.Equal(Guid.Empty, page.AccessToken);
        }
    }
}
