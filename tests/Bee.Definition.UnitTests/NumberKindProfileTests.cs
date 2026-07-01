using System.ComponentModel;

namespace Bee.Definition.UnitTests
{
    public class NumberKindProfileTests
    {
        [Theory]
        [InlineData(NumberKind.Quantity, 'N')]
        [InlineData(NumberKind.Weight, 'N')]
        [InlineData(NumberKind.Amount, 'N')]
        [InlineData(NumberKind.UnitPrice, 'N')]
        [InlineData(NumberKind.Cost, 'N')]
        [InlineData(NumberKind.ExchangeRate, 'N')]
        [InlineData(NumberKind.Percent, 'P')]
        [DisplayName("GetFormatLetter 百分比回 P、其餘回 N")]
        public void GetFormatLetter_ReturnsExpectedLetter(NumberKind kind, char expected)
        {
            Assert.Equal(expected, NumberKindProfile.GetFormatLetter(kind));
        }

        [Theory]
        [InlineData(NumberKind.Quantity, 0)]
        [InlineData(NumberKind.Weight, 3)]
        [InlineData(NumberKind.Amount, 2)]
        [InlineData(NumberKind.Percent, 2)]
        [InlineData(NumberKind.UnitPrice, 4)]
        [InlineData(NumberKind.Cost, 4)]
        [InlineData(NumberKind.ExchangeRate, 5)]
        [DisplayName("GetDefaultDecimals 回傳契約表的框架預設位數")]
        public void GetDefaultDecimals_ReturnsContractDefaults(NumberKind kind, int expected)
        {
            Assert.Equal(expected, NumberKindProfile.GetDefaultDecimals(kind));
        }

        [Theory]
        [InlineData(NumberKind.Quantity, RoundingPolicy.Round)]
        [InlineData(NumberKind.Weight, RoundingPolicy.Round)]
        [InlineData(NumberKind.Amount, RoundingPolicy.Round)]
        [InlineData(NumberKind.Percent, RoundingPolicy.Round)]
        [InlineData(NumberKind.UnitPrice, RoundingPolicy.Preserve)]
        [InlineData(NumberKind.Cost, RoundingPolicy.Preserve)]
        [InlineData(NumberKind.ExchangeRate, RoundingPolicy.Preserve)]
        [DisplayName("GetRoundingPolicy 四捨五入類回 Round、不捨入類回 Preserve")]
        public void GetRoundingPolicy_ReturnsExpectedPolicy(NumberKind kind, RoundingPolicy expected)
        {
            Assert.Equal(expected, NumberKindProfile.GetRoundingPolicy(kind));
        }

        [Theory]
        [InlineData(NumberKind.Amount, DecimalsSource.Currency)]
        [InlineData(NumberKind.Quantity, DecimalsSource.Unit)]
        [InlineData(NumberKind.Weight, DecimalsSource.Unit)]
        [InlineData(NumberKind.ExchangeRate, DecimalsSource.SystemFixed)]
        [InlineData(NumberKind.Percent, DecimalsSource.Company)]
        [InlineData(NumberKind.UnitPrice, DecimalsSource.Company)]
        [InlineData(NumberKind.Cost, DecimalsSource.Company)]
        [DisplayName("GetDecimalsSource 回傳契約表的位數來源")]
        public void GetDecimalsSource_ReturnsContractSource(NumberKind kind, DecimalsSource expected)
        {
            Assert.Equal(expected, NumberKindProfile.GetDecimalsSource(kind));
        }

        [Theory]
        [InlineData(NumberKind.Quantity, 0, "N0")]
        [InlineData(NumberKind.Amount, 2, "N2")]
        [InlineData(NumberKind.Cost, 4, "N4")]
        [InlineData(NumberKind.ExchangeRate, 5, "N5")]
        [InlineData(NumberKind.Percent, 2, "P2")]
        [DisplayName("BuildFormatString 由種類與位數組出格式字串")]
        public void BuildFormatString_ComposesLetterAndDecimals(NumberKind kind, int decimals, string expected)
        {
            Assert.Equal(expected, NumberKindProfile.BuildFormatString(kind, decimals));
        }
    }
}
