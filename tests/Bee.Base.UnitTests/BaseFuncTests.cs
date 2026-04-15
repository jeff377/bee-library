using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class BaseFuncTests
    {
        [Fact]
        [DisplayName("IsNumeric 應正確判斷各種型別的數值性")]
        public void IsNumeric_VariousTypes_ReturnsExpectedResult()
        {
            // 布林值測試
            Assert.True(BaseFunc.IsNumeric(true));
            Assert.True(BaseFunc.IsNumeric(false));

            // 列舉型別測試
            Assert.True(BaseFunc.IsNumeric(DateInterval.Day));
            Assert.True(BaseFunc.IsNumeric(DateInterval.Hour));

            // 數值型別測試
            Assert.True(BaseFunc.IsNumeric(123)); // 整數
            Assert.True(BaseFunc.IsNumeric(123.45)); // 浮點數
            Assert.True(BaseFunc.IsNumeric(123.45m)); // 十進位數

            // 字串型別測試
            Assert.True(BaseFunc.IsNumeric("123"));
            Assert.True(BaseFunc.IsNumeric("123.45"));
            Assert.False(BaseFunc.IsNumeric("abc"));

            // 特殊值測試
            Assert.False(BaseFunc.IsNumeric(null));
            Assert.False(BaseFunc.IsNumeric(new object()));
            Assert.False(BaseFunc.IsNumeric(DateTime.Now));
        }

        [Fact]
        [DisplayName("RndInt 應回傳指定範圍內的隨機整數")]
        public void RndInt_ReturnsValueWithinRange()
        {
            int min = 1;
            int max = 10;

            for (int i = 0; i < 100; i++)
            {
                int value = BaseFunc.RndInt(min, max);
                Assert.InRange(value, min, max - 1);
            }
        }
    }
}
