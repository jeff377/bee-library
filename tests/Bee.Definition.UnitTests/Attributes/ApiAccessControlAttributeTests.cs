using System.ComponentModel;
using Bee.Definition.Attributes;
using Bee.Definition.Security;

namespace Bee.Definition.UnitTests.Attributes
{
    /// <summary>
    /// ApiAccessControlAttribute 建構子與屬性測試。
    /// </summary>
    public class ApiAccessControlAttributeTests
    {
        [Theory]
        [InlineData(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        [InlineData(ApiProtectionLevel.Encoded, ApiAccessRequirement.Authenticated)]
        [InlineData(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        [DisplayName("建構子應將 ProtectionLevel 與 AccessRequirement 原樣儲存")]
        public void Constructor_SetsProperties(ApiProtectionLevel level, ApiAccessRequirement requirement)
        {
            // Act
            var attr = new ApiAccessControlAttribute(level, requirement);

            // Assert
            Assert.Equal(level, attr.ProtectionLevel);
            Assert.Equal(requirement, attr.AccessRequirement);
        }

        [Fact]
        [DisplayName("建構子省略 AccessRequirement 時預設應為 Authenticated")]
        public void Constructor_DefaultAccessRequirement_IsAuthenticated()
        {
            // Act
            var attr = new ApiAccessControlAttribute(ApiProtectionLevel.Encrypted);

            // Assert
            Assert.Equal(ApiAccessRequirement.Authenticated, attr.AccessRequirement);
        }
    }
}
