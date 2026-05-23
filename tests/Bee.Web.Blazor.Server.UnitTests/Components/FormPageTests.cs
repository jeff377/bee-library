using System.ComponentModel;
using System.Reflection;
using Bee.Web.Blazor.Server.Components;
using Bee.Web.Blazor.Server.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 針對 <see cref="FormPage"/> 的結構性 smoke 測試。
    /// 確認公開參數屬性、CascadingParameter、[Inject] 注入與預設值等編譯期宣告正確；
    /// OnInitializedAsync 等方法需 Blazor 渲染器驅動，留待 Phase 2 bUnit 整合測試覆蓋。
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
        [DisplayName("ProgId 屬性同時標有 [Parameter] 及 [EditorRequired]")]
        public void ProgId_HasParameterAndEditorRequiredAttributes()
        {
            var property = GetPublicProperty(nameof(FormPage.ProgId));
            Assert.NotNull(property.GetCustomAttribute<ParameterAttribute>());
            Assert.NotNull(property.GetCustomAttribute<EditorRequiredAttribute>());
        }

        [Fact]
        [DisplayName("AccessToken 屬性標有 [CascadingParameter]")]
        public void AccessToken_HasCascadingParameterAttribute()
        {
            var property = GetPublicProperty(nameof(FormPage.AccessToken));
            Assert.NotNull(property.GetCustomAttribute<CascadingParameterAttribute>());
        }

        [Fact]
        [DisplayName("Factory 私有屬性標有 [Inject] 且型別為 BeeApiConnectorFactory")]
        public void Factory_IsInjectedBeeApiConnectorFactory()
        {
            var property = GetNonPublicProperty("Factory");
            Assert.NotNull(property.GetCustomAttribute<InjectAttribute>());
            Assert.Equal(typeof(BeeApiConnectorFactory), property.PropertyType);
        }

        [Fact]
        [DisplayName("FormPage 預設 ProgId 為空字串")]
        public void ProgId_Default_IsEmptyString()
        {
            var page = new FormPage();
            Assert.Equal(string.Empty, page.ProgId);
        }

        [Fact]
        [DisplayName("FormPage 預設 AccessToken 為 Guid.Empty")]
        public void AccessToken_Default_IsGuidEmpty()
        {
            var page = new FormPage();
            Assert.Equal(Guid.Empty, page.AccessToken);
        }
    }
}
