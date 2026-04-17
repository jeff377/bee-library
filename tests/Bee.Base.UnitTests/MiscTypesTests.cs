using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Base.UnitTests
{
    public class ApiExceptionTests
    {
        [Fact]
        [DisplayName("無參建構子應產生空訊息與未處理狀態")]
        public void DefaultCtor_InitializesEmpty()
        {
            var ex = new ApiException();
            Assert.Equal(string.Empty, ex.Message);
            Assert.Equal(string.Empty, ex.StackTrace);
            Assert.False(ex.IsHandle);
            Assert.Equal(string.Empty, ex.ToString());
        }

        [Fact]
        [DisplayName("由例外建構時應複製 Message，預設不包含 StackTrace")]
        public void FromException_CopiesMessage_OmitsStackTraceByDefault()
        {
            InvalidOperationException inner;
            try { throw new InvalidOperationException("boom"); }
            catch (InvalidOperationException e) { inner = e; }

            var api = new ApiException(inner);

            Assert.Equal("boom", api.Message);
            Assert.Equal(string.Empty, api.StackTrace);
            Assert.Equal("boom", api.ToString());
        }

        [Fact]
        [DisplayName("由例外建構且允許 StackTrace 時應填入非空堆疊")]
        public void FromException_IncludeStackTrace_PopulatesStackTrace()
        {
            InvalidOperationException inner;
            try { throw new InvalidOperationException("boom"); }
            catch (InvalidOperationException e) { inner = e; }

            var api = new ApiException(inner, includeStackTrace: true);

            Assert.False(string.IsNullOrEmpty(api.StackTrace));
        }
    }

    public class TreeNodeAttributeTests
    {
        [TreeNode("Literal Label")]
        private sealed class LiteralClass { }

        [TreeNode("{0}-{1}", "Name,Age")]
        private sealed class FormattedClass
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        [TreeNode("{0}", "Name")]
        private sealed class EmptyFormattedClass : IDisplayName
        {
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }

        private sealed class NoAttrClass
        {
            public override string ToString() => "no-attr-tostring";
        }

        [TreeNode("label", collectionFolder: true)]
        private sealed class CollectionFolderClass { }

        [Fact]
        [DisplayName("GetDisplayText 於 Attribute 為字面字串時應直接回傳")]
        public void GetDisplayText_Literal_ReturnsDisplayFormat()
        {
            Assert.Equal("Literal Label", TreeNodeAttribute.GetDisplayText(new LiteralClass()));
        }

        [Fact]
        [DisplayName("GetDisplayText 於 Attribute 含 PropertyName 應格式化屬性值")]
        public void GetDisplayText_WithPropertyName_FormatsValues()
        {
            var obj = new FormattedClass { Name = "Alice", Age = 30 };
            Assert.Equal("Alice-30", TreeNodeAttribute.GetDisplayText(obj));
        }

        [Fact]
        [DisplayName("GetDisplayText 無 Attribute 時應回傳 ToString")]
        public void GetDisplayText_NoAttribute_ReturnsToString()
        {
            Assert.Equal("no-attr-tostring", TreeNodeAttribute.GetDisplayText(new NoAttrClass()));
        }

        [Fact]
        [DisplayName("GetDisplayText 格式化結果為空且實作 IDisplayName 時應回傳 DisplayName")]
        public void GetDisplayText_EmptyFormattedValue_FallsBackToIDisplayName()
        {
            var obj = new EmptyFormattedClass { Name = string.Empty, DisplayName = "fallback" };
            Assert.Equal("fallback", TreeNodeAttribute.GetDisplayText(obj));
        }

        [Fact]
        [DisplayName("TreeNodeAttribute 建構子應保存 CollectionFolder 與 DisplayFormat")]
        public void Ctor_CollectionFolder_StoresFlag()
        {
            var attr = new TreeNodeAttribute("label", collectionFolder: true);
            Assert.Equal("label", attr.DisplayFormat);
            Assert.True(attr.CollectionFolder);
        }

        [Fact]
        [DisplayName("TreeNodeAttribute 預設建構子應初始化空字串與 false 預設值")]
        public void Ctor_Parameterless_Defaults()
        {
            var attr = new TreeNodeAttribute();
            Assert.Equal(string.Empty, attr.DisplayFormat);
            Assert.Equal(string.Empty, attr.PropertyName);
            Assert.False(attr.CollectionFolder);
        }

        [Fact]
        [DisplayName("TreeNodeIgnoreAttribute 應可被建立")]
        public void TreeNodeIgnoreAttribute_CanBeInstantiated()
        {
            Assert.NotNull(new TreeNodeIgnoreAttribute());
        }
    }

    public class EnumDefaultsTests
    {
        [Fact]
        [DisplayName("DefaultBoolean 預設值為 Default")]
        public void DefaultBoolean_DefaultValue_IsDefault()
        {
            DefaultBoolean value = default;
            Assert.Equal(DefaultBoolean.Default, value);
        }

        [Fact]
        [DisplayName("NotSetBoolean 預設值為 NotSet")]
        public void NotSetBoolean_DefaultValue_IsNotSet()
        {
            NotSetBoolean value = default;
            Assert.Equal(NotSetBoolean.NotSet, value);
        }

        [Fact]
        [DisplayName("DateInterval 預設值為 Year")]
        public void DateInterval_DefaultValue_IsYear()
        {
            DateInterval value = default;
            Assert.Equal(DateInterval.Year, value);
        }
    }
}
