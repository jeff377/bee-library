using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    public class NumberFormatPresetsTests
    {
        [Theory]
        [InlineData("Quantity", "N0")]
        [InlineData("UnitPrice", "N2")]
        [InlineData("Amount", "N2")]
        [InlineData("Cost", "N4")]
        [InlineData("quantity", "N0")]
        [DisplayName("ToFormatString 已知格式名稱應回傳對應格式字串")]
        public void ToFormatString_KnownPreset_ReturnsExpectedString(string preset, string expected)
        {
            var result = NumberFormatPresets.ToFormatString(preset);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Unknown")]
        [InlineData(null)]
        [DisplayName("ToFormatString 空或未知格式應回傳空字串")]
        public void ToFormatString_EmptyOrUnknown_ReturnsEmpty(string? preset)
        {
            var result = NumberFormatPresets.ToFormatString(preset!);

            Assert.Equal(string.Empty, result);
        }
    }
}
