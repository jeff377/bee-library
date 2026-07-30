using System.ComponentModel;
using System.Reflection;
using Bee.Analyzers.Definitions;
using Bee.Definition.Database;

namespace Bee.Analyzers.UnitTests.Definitions
{
    /// <summary>
    /// analyzer 內硬編碼的 scope 清單與框架常數的同步斷言。
    /// </summary>
    /// <remarks>
    /// analyzer 專案為 netstandard2.0，無法引用 net10.0 的 <c>Bee.Definition</c>，因此
    /// <see cref="DbCategoryScopes"/> 只能複製 <see cref="DbCategoryIds"/> 的值。本測試專案同時引用
    /// 兩者，作為漂移閘門：框架新增 scope 而 analyzer 未同步時，此測試立即失敗。
    /// </remarks>
    public class DbCategoryScopesSyncTests
    {
        [Fact]
        [DisplayName("analyzer 的 scope 清單必須與框架 DbCategoryIds 完全一致")]
        public void All_MatchesFrameworkConstants()
        {
            // Arrange
            var frameworkScopes = typeof(DbCategoryIds)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            // Act
            var analyzerScopes = DbCategoryScopes.All
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            // Assert
            Assert.Equal(frameworkScopes, analyzerScopes);
        }

        [Fact]
        [DisplayName("框架常數應被 IsValid 全數接受")]
        public void IsValid_AcceptsEveryFrameworkConstant()
        {
            // Assert
            Assert.True(DbCategoryScopes.IsValid(DbCategoryIds.Common));
            Assert.True(DbCategoryScopes.IsValid(DbCategoryIds.Company));
            Assert.True(DbCategoryScopes.IsValid(DbCategoryIds.Log));
        }
    }
}
