using System.ComponentModel;

namespace Bee.Expressions.UnitTests
{
    /// <summary>
    /// <see cref="DynamicExpressoEvaluator"/> 測試：算術 / 布林條件 / 型別轉換 / 輔助函式 /
    /// 相依變數偵測 / 編譯快取一致性 / 沙箱阻擋。
    /// </summary>
    public class DynamicExpressoEvaluatorTests
    {
        private readonly DynamicExpressoEvaluator _evaluator = new();

        private static Dictionary<string, object?> Vars(params (string Name, object? Value)[] pairs)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (name, value) in pairs) { dict[name] = value; }
            return dict;
        }

        [Fact]
        [DisplayName("欄位算術：unit_price * qty 應回傳乘積（decimal）")]
        public void Evaluate_Arithmetic_ReturnsProduct()
        {
            var result = _evaluator.Evaluate<decimal>(
                "unit_price * qty", Vars(("unit_price", 10m), ("qty", 3m)));

            Assert.Equal(30m, result);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(-1, false)]
        [DisplayName("布林條件：amount > 0 依值回傳 true/false")]
        public void Evaluate_BoolCondition_ReturnsExpected(int amount, bool expected)
        {
            var result = _evaluator.Evaluate<bool>("amount > 0", Vars(("amount", (decimal)amount)));

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("回傳型別轉換：int 運算結果轉為 decimal 回傳型別")]
        public void Evaluate_IntExpressionToDecimalReturn_Converts()
        {
            var result = _evaluator.Evaluate<decimal>("qty * 2", Vars(("qty", 3)));

            Assert.IsType<decimal>(result);
            Assert.Equal(6m, result);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("x", false)]
        [DisplayName("字串輔助函式：IsNullOrEmpty(name) 依值回傳")]
        public void Evaluate_StringHelperFunction_ReturnsExpected(string name, bool expected)
        {
            var result = _evaluator.Evaluate<bool>("IsNullOrEmpty(name)", Vars(("name", name)));

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("常數運算式（無變數）：Today() 回傳今日日期")]
        public void Evaluate_TodayFunction_NoVariables_ReturnsToday()
        {
            var result = _evaluator.Evaluate<DateTime>(
                "Today()", new Dictionary<string, object?>(StringComparer.Ordinal));

            Assert.Equal(DateTime.Today, result);
        }

        [Fact]
        [DisplayName("相依偵測：GetReferencedVariables 回傳欄位變數、排除已註冊函式")]
        public void GetReferencedVariables_ReturnsFieldNamesOnly()
        {
            var referenced = _evaluator.GetReferencedVariables("qty > 0 && Today() > order_date");

            Assert.Contains("qty", referenced);
            Assert.Contains("order_date", referenced);
            // Today 是已註冊函式，不應被視為未知變數
            Assert.DoesNotContain("Today", referenced);
        }

        [Fact]
        [DisplayName("編譯快取：同一運算式重複求值，不同輸入皆得正確結果")]
        public void Evaluate_SameExpressionReused_ProducesCorrectResultsAcrossInputs()
        {
            var first = _evaluator.Evaluate<decimal>(
                "unit_price * qty", Vars(("unit_price", 10m), ("qty", 2m)));
            var second = _evaluator.Evaluate<decimal>(
                "unit_price * qty", Vars(("unit_price", 7m), ("qty", 3m)));

            Assert.Equal(20m, first);
            Assert.Equal(21m, second);
        }

        [Theory]
        [InlineData("11111111-1111-1111-1111-111111111111", true)]
        [InlineData("00000000-0000-0000-0000-000000000000", false)]
        [DisplayName("Guid 支援：customer_rowid != Guid.Empty 依值回傳")]
        public void Evaluate_GuidComparison_ReturnsExpected(string guid, bool expected)
        {
            var result = _evaluator.Evaluate<bool>(
                "customer_rowid != Guid.Empty", Vars(("customer_rowid", Guid.Parse(guid))));

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("沙箱：存取未註冊型別（System.IO.File）應拋 ExpressionEvaluationException")]
        public void Evaluate_UnregisteredType_ThrowsExpressionEvaluationException()
        {
            var ex = Assert.Throws<ExpressionEvaluationException>(() =>
                _evaluator.Evaluate<string>(
                    "System.IO.File.ReadAllText(\"secret.txt\")",
                    new Dictionary<string, object?>(StringComparer.Ordinal)));

            Assert.Equal("System.IO.File.ReadAllText(\"secret.txt\")", ex.Expression);
        }

        [Fact]
        [DisplayName("語法錯誤運算式應拋 ExpressionEvaluationException")]
        public void Evaluate_MalformedExpression_ThrowsExpressionEvaluationException()
        {
            Assert.Throws<ExpressionEvaluationException>(() =>
                _evaluator.Evaluate<decimal>("unit_price *", Vars(("unit_price", 1m))));
        }
    }
}
