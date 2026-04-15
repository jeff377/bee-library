using System.ComponentModel;
using Bee.Definition.Forms;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    public class DefineFuncTests
    {
        [Theory]
        [InlineData(DefineType.SystemSettings, typeof(SystemSettings))]
        [InlineData(DefineType.DatabaseSettings, typeof(DatabaseSettings))]
        [InlineData(DefineType.FormSchema, typeof(FormSchema))]
        [DisplayName("GetDefineType 傳入有效定義類型應回傳正確的型別")]
        public void GetDefineType_ValidType_ReturnsExpectedType(DefineType defineType, Type expectedType)
        {
            // Act
            var result = DefineFunc.GetDefineType(defineType);

            // Assert
            Assert.Equal(expectedType, result);
        }
    }
}
