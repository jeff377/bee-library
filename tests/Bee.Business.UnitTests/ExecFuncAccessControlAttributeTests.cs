using System.ComponentModel;
using Bee.Business.Attributes;
using Bee.Definition.Security;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="ExecFuncAccessControlAttribute"/> 建構子與屬性測試。
    /// </summary>
    public class ExecFuncAccessControlAttributeTests
    {
        [Fact]
        [DisplayName("預設建構子的 AccessRequirement 應為 Authenticated")]
        public void DefaultConstructor_ReturnsAuthenticated()
        {
            var attr = new ExecFuncAccessControlAttribute();
            Assert.Equal(ApiAccessRequirement.Authenticated, attr.AccessRequirement);
        }

        [Theory]
        [InlineData(ApiAccessRequirement.Anonymous)]
        [InlineData(ApiAccessRequirement.Authenticated)]
        [DisplayName("建構子傳入的 AccessRequirement 應保留")]
        public void Constructor_PreservesAccessRequirement(ApiAccessRequirement requirement)
        {
            var attr = new ExecFuncAccessControlAttribute(requirement);
            Assert.Equal(requirement, attr.AccessRequirement);
        }
    }
}
