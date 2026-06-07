using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    public class DefineTypeExtensionsTests
    {
        [Theory]
        [InlineData(DefineType.SystemSettings, typeof(SystemSettings))]
        [InlineData(DefineType.DatabaseSettings, typeof(DatabaseSettings))]
        [InlineData(DefineType.DbCategorySettings, typeof(DbCategorySettings))]
        [InlineData(DefineType.ProgramSettings, typeof(ProgramSettings))]
        [InlineData(DefineType.TableSchema, typeof(TableSchema))]
        [InlineData(DefineType.FormSchema, typeof(FormSchema))]
        [InlineData(DefineType.FormLayout, typeof(FormLayout))]
        [InlineData(DefineType.PermissionModels, typeof(PermissionModels))]
        [DisplayName("ToClrType 傳入有效定義類型應回傳正確的型別")]
        public void ToClrType_ValidType_ReturnsExpectedType(DefineType defineType, Type expectedType)
        {
            var result = defineType.ToClrType();

            Assert.Equal(expectedType, result);
        }

        [Fact]
        [DisplayName("ToClrType 傳入未支援類型應拋出 NotSupportedException")]
        public void ToClrType_UnsupportedType_ThrowsNotSupportedException()
        {
            var invalid = (DefineType)999;

            Assert.Throws<NotSupportedException>(() => invalid.ToClrType());
        }

        [Fact]
        [DisplayName("ToClrType Language 未在映射字典中應拋出 NotSupportedException")]
        public void ToClrType_Language_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => DefineType.Language.ToClrType());
        }
    }
}
