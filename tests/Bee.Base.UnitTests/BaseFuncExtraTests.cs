using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

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

            var desc = Assert.IsAssignableFrom<DescriptionAttribute>(attr);
            Assert.Equal("Holder-Component", desc.Description);
        }

        [Fact]
        [DisplayName("GetPropertyAttribute 屬性存在且有對應 Attribute 應回傳實例")]
        public void GetPropertyAttribute_ExistingAttribute_ReturnsInstance()
        {
            var component = new HolderComponent();

            var attr = BaseFunc.GetPropertyAttribute(component, "Number", typeof(DefaultValueAttribute));

            var def = Assert.IsAssignableFrom<DefaultValueAttribute>(attr);
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
    }
}
