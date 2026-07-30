using System.ComponentModel;
using Bee.Analyzers.Definitions;
using Bee.Base.Data;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// analyzer 內硬編碼的欄位型別清單與框架列舉的同步斷言。
    /// </summary>
    /// <remarks>
    /// analyzer 專案為 netstandard2.0，無法引用 net10.0 的框架組件，因此
    /// <see cref="FieldDbTypes"/> 只能複製 <see cref="FieldDbType"/> 的成員名稱。本測試作為漂移閘門：
    /// 列舉新增成員而 analyzer 未同步時立即失敗。
    /// </remarks>
    public class FieldDbTypesSyncTests
    {
        [Fact]
        [DisplayName("analyzer 的欄位型別清單必須與框架 FieldDbType 列舉完全一致")]
        public void All_MatchesFrameworkEnum()
        {
            // Arrange
            var frameworkNames = Enum.GetNames<FieldDbType>()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            // Act
            var analyzerNames = FieldDbTypes.All
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            // Assert
            Assert.Equal(frameworkNames, analyzerNames);
        }

        [Theory]
        [InlineData(FieldDbType.String)]
        [InlineData(FieldDbType.Currency)]
        [InlineData(FieldDbType.AutoIncrement)]
        [InlineData(FieldDbType.Guid)]
        [DisplayName("框架列舉成員應被 IsValid 接受")]
        public void IsValid_AcceptsFrameworkMembers(FieldDbType dbType)
        {
            // Assert
            Assert.True(FieldDbTypes.IsValid(dbType.ToString()));
        }

        [Fact]
        [DisplayName("僅大小寫不符時應可找出正確拼法")]
        public void FindCaseInsensitiveMatch_ReturnsCorrectCasing()
        {
            // Assert
            Assert.Equal("String", FieldDbTypes.FindCaseInsensitiveMatch("string"));
            Assert.Equal("DateTime", FieldDbTypes.FindCaseInsensitiveMatch("datetime"));
            Assert.Null(FieldDbTypes.FindCaseInsensitiveMatch("Varchar"));
        }
    }
}
