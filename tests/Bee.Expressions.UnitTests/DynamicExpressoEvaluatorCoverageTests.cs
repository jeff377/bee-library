using System.ComponentModel;

namespace Bee.Expressions.UnitTests
{
    /// <summary>
    /// <see cref="DynamicExpressoEvaluator"/> 覆蓋率補強測試：null 變數值分支、null 回傳值、
    /// GetReferencedVariables 的解析失敗與零 / 多重識別字邊界。
    /// </summary>
    public class DynamicExpressoEvaluatorCoverageTests
    {
        private readonly DynamicExpressoEvaluator _evaluator = new();

        private static Dictionary<string, object?> Vars(params (string Name, object? Value)[] pairs)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (name, value) in pairs) { dict[name] = value; }
            return dict;
        }

        [Fact]
        [DisplayName("null 變數值：型別退回 object、參數值退回空字串，運算式仍可求值")]
        public void Evaluate_NullVariableValue_UsesObjectTypeAndEmptyStringArgument()
        {
            // A null variable value drives the `?? typeof(object)` type fallback (GetOrCompile /
            // BuildCacheKey) and the `?? (object)string.Empty` argument fallback.
            var result = _evaluator.Evaluate<bool>("name == null", Vars(("name", null)));

            // The null argument is replaced with string.Empty, so the comparison is false.
            Assert.False(result);
        }

        [Fact]
        [DisplayName("Evaluate<T>：運算式回傳 null 時應回傳 default")]
        public void EvaluateGeneric_NullResult_ReturnsDefault()
        {
            var result = _evaluator.Evaluate<string?>(
                "null", new Dictionary<string, object?>(StringComparer.Ordinal));

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("GetReferencedVariables：多個未知識別字皆被偵測")]
        public void GetReferencedVariables_MultipleIdentifiers_ReturnsAll()
        {
            var referenced = _evaluator.GetReferencedVariables("a + b + c");

            Assert.Contains("a", referenced);
            Assert.Contains("b", referenced);
            Assert.Contains("c", referenced);
        }

        [Fact]
        [DisplayName("GetReferencedVariables：無變數的常數運算式回傳空集合")]
        public void GetReferencedVariables_NoIdentifiers_ReturnsEmpty()
        {
            var referenced = _evaluator.GetReferencedVariables("1 + 2");

            Assert.Empty(referenced);
        }
    }
}
