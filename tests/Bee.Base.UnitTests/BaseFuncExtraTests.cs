using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// BaseFunc 補強測試：Reflection 相關、泛型判斷、型別檢查、
    /// 參數驗證與例外展開。
    /// </summary>
    public class BaseFuncExtraTests
    {
        [Description("Holder-Component")]
        private class HolderComponent
        {
            [DefaultValue(42)]
            public int Number { get; set; } = 7;

            public string Name { get; set; } = "init";
        }

        private class GenericBase<T>
        {
            public T? Value { get; set; }
        }

        private class GenericDerived : GenericBase<int> { }

        [Fact]
        [DisplayName("IsGenericType 直接為泛型型別時應回傳 true")]
        public void IsGenericType_DirectGenericType_ReturnsTrue()
        {
            var list = new List<int>();
            Assert.True(BaseFunc.IsGenericType(list, typeof(List<>)));
        }

        [Fact]
        [DisplayName("IsGenericType 繼承自泛型型別時應回傳 true")]
        public void IsGenericType_DerivedFromGeneric_ReturnsTrue()
        {
            var derived = new GenericDerived();
            Assert.True(BaseFunc.IsGenericType(derived, typeof(GenericBase<>)));
        }

        [Fact]
        [DisplayName("IsGenericType 非泛型型別應回傳 false")]
        public void IsGenericType_NonGenericType_ReturnsFalse()
        {
            Assert.False(BaseFunc.IsGenericType("text", typeof(List<>)));
        }

        [Fact]
        [DisplayName("IsGenericType value 為 null 應拋出 ArgumentNullException")]
        public void IsGenericType_NullValue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BaseFunc.IsGenericType(null!, typeof(List<>)));
        }

        [Fact]
        [DisplayName("IsGenericType genericType 為 null 應拋出 ArgumentNullException")]
        public void IsGenericType_NullGenericType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BaseFunc.IsGenericType(new object(), null!));
        }

        [Fact]
        [DisplayName("CheckTypes 符合任一型別應回傳 true")]
        public void CheckTypes_AnyMatch_ReturnsTrue()
        {
            Assert.True(BaseFunc.CheckTypes("abc", typeof(int), typeof(string)));
        }

        [Fact]
        [DisplayName("CheckTypes 無相符型別應回傳 false")]
        public void CheckTypes_NoMatch_ReturnsFalse()
        {
            Assert.False(BaseFunc.CheckTypes(123, typeof(string), typeof(DateTime)));
        }

        [Fact]
        [DisplayName("GetAttribute 應回傳元件上的 Attribute")]
        public void GetAttribute_ExistingAttribute_ReturnsInstance()
        {
            var component = new HolderComponent();

            var attr = BaseFunc.GetAttribute(component, typeof(DescriptionAttribute));

            var desc = Assert.IsType<DescriptionAttribute>(attr, exactMatch: false);
            Assert.Equal("Holder-Component", desc.Description);
        }

        [Fact]
        [DisplayName("GetPropertyAttribute 屬性存在且有對應 Attribute 應回傳實例")]
        public void GetPropertyAttribute_ExistingAttribute_ReturnsInstance()
        {
            var component = new HolderComponent();

            var attr = BaseFunc.GetPropertyAttribute(component, "Number", typeof(DefaultValueAttribute));

            var def = Assert.IsType<DefaultValueAttribute>(attr, exactMatch: false);
            Assert.Equal(42, def.Value);
        }

        [Fact]
        [DisplayName("GetPropertyAttribute 屬性不存在應回傳 null")]
        public void GetPropertyAttribute_UnknownProperty_ReturnsNull()
        {
            var component = new HolderComponent();

            var attr = BaseFunc.GetPropertyAttribute(component, "DoesNotExist", typeof(DefaultValueAttribute));

            Assert.Null(attr);
        }

        [Fact]
        [DisplayName("GetPropertyValue 應回傳當前屬性值")]
        public void GetPropertyValue_ExistingProperty_ReturnsValue()
        {
            var component = new HolderComponent { Name = "Alice" };

            var value = BaseFunc.GetPropertyValue(component, "Name");

            Assert.Equal("Alice", value);
        }

        [Fact]
        [DisplayName("GetPropertyValue 屬性不存在應回傳 null")]
        public void GetPropertyValue_UnknownProperty_ReturnsNull()
        {
            var component = new HolderComponent();

            var value = BaseFunc.GetPropertyValue(component, "DoesNotExist");

            Assert.Null(value);
        }

        [Fact]
        [DisplayName("SetPropertyValue 應指派屬性值至元件")]
        public void SetPropertyValue_ExistingProperty_SetsValue()
        {
            var component = new HolderComponent();

            BaseFunc.SetPropertyValue(component, "Name", "Bob");

            Assert.Equal("Bob", component.Name);
        }

        [Fact]
        [DisplayName("SetPropertyValue 屬性不存在時應安全略過")]
        public void SetPropertyValue_UnknownProperty_IsSilent()
        {
            var component = new HolderComponent();

            // 僅需不擲例外即可；屬性不存在時是 no-op
            var ex = Record.Exception(() => BaseFunc.SetPropertyValue(component, "DoesNotExist", "x"));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("EnsureNotNullOrWhiteSpace 所有參數皆有效時不應擲例外")]
        public void EnsureNotNullOrWhiteSpace_ValidParameters_DoesNotThrow()
        {
            var ex = Record.Exception(() => BaseFunc.EnsureNotNullOrWhiteSpace(
                ("abc", "p1"), (123, "p2")));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("EnsureNotNullOrWhiteSpace value 為 null 應拋 ArgumentException 並包含參數名")]
        public void EnsureNotNullOrWhiteSpace_NullValue_ThrowsWithParamName()
        {
            var ex = Assert.Throws<ArgumentException>(() => BaseFunc.EnsureNotNullOrWhiteSpace(
                (null!, "first"), ("ok", "second")));
            Assert.Equal("first", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("EnsureNotNullOrWhiteSpace 字串為空白應拋 ArgumentException 並包含參數名")]
        public void EnsureNotNullOrWhiteSpace_WhitespaceString_ThrowsWithParamName(string value)
        {
            var ex = Assert.Throws<ArgumentException>(() => BaseFunc.EnsureNotNullOrWhiteSpace(
                (value, "blank")));
            Assert.Equal("blank", ex.ParamName);
        }

        [Fact]
        [DisplayName("UnwrapException 於單層 Exception 應回傳原例外")]
        public void UnwrapException_PlainException_ReturnsSelf()
        {
            var ex = new InvalidOperationException("plain");
            Assert.Same(ex, BaseFunc.UnwrapException(ex));
        }

        [Fact]
        [DisplayName("UnwrapException 於 AggregateException 應回傳第一個 inner exception")]
        public void UnwrapException_AggregateException_ReturnsFirstInner()
        {
            var inner1 = new InvalidOperationException("inner1");
            var inner2 = new NotSupportedException("inner2");
            var agg = new AggregateException(inner1, inner2);

            var result = BaseFunc.UnwrapException(agg);

            Assert.Same(inner1, result);
        }

        [Fact]
        [DisplayName("UnwrapException 於 TargetInvocationException 應回傳 inner exception")]
        public void UnwrapException_TargetInvocation_ReturnsInner()
        {
            var inner = new InvalidOperationException("inner");
            var wrapper = new TargetInvocationException(inner);

            var result = BaseFunc.UnwrapException(wrapper);

            Assert.Same(inner, result);
        }

        [Fact]
        [DisplayName("UnwrapException 應遞迴展開多層包裝")]
        public void UnwrapException_NestedWrappers_UnwrapsFully()
        {
            var core = new InvalidOperationException("core");
            var target = new TargetInvocationException(core);
            var agg = new AggregateException(target);

            var result = BaseFunc.UnwrapException(agg);

            Assert.Same(core, result);
        }

        [Fact]
        [DisplayName("UnwrapException 於 null 應拋出 ArgumentNullException")]
        public void UnwrapException_NullException_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BaseFunc.UnwrapException(null!));
        }

        [Theory]
        [InlineData("12345", 5, true)]
        [InlineData("12345", 4, false)]
        [InlineData("abc", 3, false)]
        [InlineData("", 0, false)]
        [DisplayName("IsNumeric(string, length) 應同時檢查數值性與長度")]
        public void IsNumeric_WithLength_ChecksBothConditions(string value, int length, bool expected)
        {
            Assert.Equal(expected, BaseFunc.IsNumeric(value, length));
        }

        private sealed class NumericToString
        {
            public override string ToString() => "3.14";
        }

        [Fact]
        [DisplayName("ConvertToNumber 對非支援型別但 ToString 為數值應回傳 double")]
        public void ConvertToNumber_NonStandardType_FallsBackToToString()
        {
            // 進入最後 double.TryParse(value.ToString(), ...) 分支
            var value = new NumericToString();
            var result = BaseFunc.ConvertToNumber(value);
            Assert.Equal(3.14d, result);
        }

        [Theory]
        [InlineData("20150312", 2015, 3, 12)] // 8-digit 西元
        [InlineData("1040312", 2015, 3, 12)]  // 7-digit 民國
        [InlineData("201503", 2015, 3, 1)]    // 6-digit 西元年月
        [InlineData("10403", 2015, 3, 1)]     // 5-digit 民國年月
        [InlineData("2015", 2015, 1, 1)]      // 4-digit 西元年
        [InlineData("104", 2015, 1, 1)]       // 3-digit 民國年
        [DisplayName("CDateTime 應依字串長度解析各種日期格式")]
        public void CDateTime_VariousLengths_ParsesCorrectly(string input, int y, int m, int d)
        {
            var result = BaseFunc.CDateTime(input);
            Assert.Equal(new DateTime(y, m, d), result);
        }

        [Fact]
        [DisplayName("CDateTime 對未支援長度的數字字串應回傳 DateTime.MinValue")]
        public void CDateTime_UnsupportedLength_ReturnsMinValue()
        {
            // 長度 2 不在 switch 的 3/4/5/6/7/8 範圍內 → default 分支 → MinValue
            var result = BaseFunc.CDateTime("12");
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        [DisplayName("CDateTime 對非數字字串經 StrToDate 返回 MinValue")]
        public void CDateTime_NonNumericString_ReturnsMinValue()
        {
            // "abcdefgh" 移除分隔字元後非數字 → StrToDate 直接回傳 MinValue
            var result = BaseFunc.CDateTime("abcdefgh");
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        [DisplayName("CDateTime 對無法轉成日期的數字字串應回傳 defaultValue")]
        public void CDateTime_InvalidCalendarDate_FallsBackToDefault()
        {
            // 20150230 → "2015-02-30" → Convert.ToDateTime 拋例外 → catch → defaultValue
            var fallback = new DateTime(2000, 1, 1);
            var result = BaseFunc.CDateTime("20150230", fallback);
            Assert.Equal(fallback, result);
        }

        [Fact]
        [DisplayName("CDate 應取得日期部分（時間為零）")]
        public void CDate_ReturnsDateOnly()
        {
            var result = BaseFunc.CDate("20150312");
            Assert.Equal(new DateTime(2015, 3, 12), result);
            Assert.Equal(TimeSpan.Zero, result.TimeOfDay);
        }

        private sealed class FakeSerializeObject : IObjectSerialize
        {
            public SerializeState SerializeState { get; private set; } = SerializeState.None;

            public void SetSerializeState(SerializeState serializeState)
            {
                SerializeState = serializeState;
            }
        }

        [Fact]
        [DisplayName("SetSerializeState 非 null 目標應委派給其 SetSerializeState 方法")]
        public void SetSerializeState_NonNull_DelegatesToTarget()
        {
            var target = new FakeSerializeObject();

            BaseFunc.SetSerializeState(target, SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, target.SerializeState);
        }

        [Fact]
        [DisplayName("SetSerializeState 目標為 null 應靜默略過")]
        public void SetSerializeState_Null_IsNoOp()
        {
            var ex = Record.Exception(() => BaseFunc.SetSerializeState(null!, SerializeState.Serialize));
            Assert.Null(ex);
        }

        private sealed class EmptySerializeObject : IObjectSerializeEmpty
        {
            public bool IsSerializeEmpty { get; set; }
        }

        [Fact]
        [DisplayName("IsSerializeEmpty state 為 None 時永遠回傳 false")]
        public void IsSerializeEmpty_StateNone_ReturnsFalse()
        {
            Assert.False(BaseFunc.IsSerializeEmpty(SerializeState.None, null!));
            Assert.False(BaseFunc.IsSerializeEmpty(SerializeState.None, new List<int>()));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 於 Serialize 狀態且 value 為 null 應回傳 true")]
        public void IsSerializeEmpty_SerializeAndNull_ReturnsTrue()
        {
            Assert.True(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, null!));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 應尊重 IObjectSerializeEmpty 回報的狀態")]
        public void IsSerializeEmpty_ObjectSerializeEmpty_ReflectsProperty()
        {
            var emptyObj = new EmptySerializeObject { IsSerializeEmpty = true };
            Assert.True(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, emptyObj));

            var notEmptyObj = new EmptySerializeObject { IsSerializeEmpty = false };
            Assert.False(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, notEmptyObj));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 於空 IList 應回傳 true,非空應回傳 false")]
        public void IsSerializeEmpty_IList_ReflectsEmptiness()
        {
            Assert.True(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, new List<int>()));
            Assert.False(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, new List<int> { 1 }));
        }

        private sealed class PureEnumerable : IEnumerable
        {
            private readonly object[] _items;
            public PureEnumerable(params object[] items) => _items = items;
            public IEnumerator GetEnumerator() => _items.GetEnumerator();
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 於 IEnumerable 應依可列舉性判斷 empty")]
        public void IsSerializeEmpty_IEnumerable_ReflectsEmptiness()
        {
            Assert.True(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, new PureEnumerable()));
            Assert.False(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, new PureEnumerable(1, 2)));
        }

        [Fact]
        [DisplayName("IsSerializeEmpty 其他型別應走 default 回傳 false")]
        public void IsSerializeEmpty_DefaultBranch_ReturnsFalse()
        {
            // int / string 等 primitive 不符合任何 case → default → false
            Assert.False(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, 123));
            Assert.False(BaseFunc.IsSerializeEmpty(SerializeState.Serialize, "abc"));
        }

    }
}
