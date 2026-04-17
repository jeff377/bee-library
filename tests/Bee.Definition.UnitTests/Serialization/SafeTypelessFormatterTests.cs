using System.ComponentModel;
using Bee.Definition.Serialization;

namespace Bee.Definition.UnitTests.Serialization
{
    /// <summary>
    /// SafeTypelessFormatter 型別白名單驗證測試。
    /// </summary>
    public class SafeTypelessFormatterTests
    {
        [Theory]
        [InlineData("System.Int32")]
        [InlineData("System.String")]
        [InlineData("System.Boolean")]
        [InlineData("System.DateTime")]
        [InlineData("System.Guid")]
        [InlineData("System.Byte[]")]
        [InlineData("System.DBNull")]
        [InlineData("System.Data.DataTable")]
        [DisplayName("IsTypeAllowed 內建原生型別應回傳 true")]
        public void IsTypeAllowed_PrimitiveTypes_ReturnsTrue(string fullName)
        {
            // Act & Assert
            Assert.True(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Theory]
        [InlineData("Bee.Base.SomeClass")]
        [InlineData("Bee.Definition.Foo")]
        [InlineData("Bee.Contracts.Dto")]
        [InlineData("Bee.Api.Core.Something")]
        [InlineData("Bee.Business.Employee")]
        [DisplayName("IsTypeAllowed Bee.* 命名空間型別應回傳 true")]
        public void IsTypeAllowed_BeeNamespaces_ReturnsTrue(string fullName)
        {
            // Act & Assert
            Assert.True(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Theory]
        [InlineData("System.Diagnostics.Process")]
        [InlineData("System.IO.File")]
        [InlineData("SomeMalicious.Attacker.Type")]
        [DisplayName("IsTypeAllowed 不在白名單的型別應回傳 false")]
        public void IsTypeAllowed_UntrustedTypes_ReturnsFalse(string fullName)
        {
            // Act & Assert
            Assert.False(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Fact]
        [DisplayName("SafeTypelessFormatter.Instance 應提供單例")]
        public void Instance_IsNotNull()
        {
            // Act & Assert
            Assert.NotNull(SafeTypelessFormatter.Instance);
        }
    }
}
