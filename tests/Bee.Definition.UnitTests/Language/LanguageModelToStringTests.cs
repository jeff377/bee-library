using System.ComponentModel;
using Bee.Definition.Language;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// 補強語言模型型別 ToString() 方法與集合無參建構子的測試覆蓋率。
    /// 涵蓋 LanguageEnum、LanguageEnumEntry、LanguageItem、LanguageResource，
    /// 以及 LanguageEnumCollection、LanguageItemCollection、LanguageEnumEntryCollection
    /// 的無參建構子路徑。
    /// </summary>
    public class LanguageModelToStringTests
    {
        [Fact]
        [DisplayName("LanguageEnum.ToString 應回傳含名稱與 Entries 數的字串")]
        public void LanguageEnum_ToString_ContainsNameAndEntryCount()
        {
            var langEnum = new LanguageEnum { Name = "Gender" };
            langEnum.Entries.Add("M", "男");
            langEnum.Entries.Add("F", "女");

            Assert.Equal("Gender (2 entries)", langEnum.ToString());
        }

        [Fact]
        [DisplayName("LanguageEnumEntry.ToString 應回傳 Code = Text 格式")]
        public void LanguageEnumEntry_ToString_FormatsCodeAndText()
        {
            var entry = new LanguageEnumEntry { Code = "M", Text = "男" };

            Assert.Equal("M = 男", entry.ToString());
        }

        [Fact]
        [DisplayName("LanguageItem.ToString 應回傳 Key = Value 格式")]
        public void LanguageItem_ToString_FormatsKeyAndValue()
        {
            var item = new LanguageItem { Key = "OK", Value = "確定" };

            Assert.Equal("OK = 確定", item.ToString());
        }

        [Fact]
        [DisplayName("LanguageResource.ToString 應包含 Namespace、Lang、Items 數與 Enums 數")]
        public void LanguageResource_ToString_ContainsAllParts()
        {
            var resource = new LanguageResource { Namespace = "Common", Lang = "zh-TW" };
            resource.Items.Add("OK", "確定");

            Assert.Equal("Common [zh-TW] (1 items, 0 enums)", resource.ToString());
        }

        [Fact]
        [DisplayName("LanguageEnumCollection 無參建構子應建立非 null 的空集合")]
        public void LanguageEnumCollection_ParameterlessConstructor_CreatesEmptyCollection()
        {
            var collection = new LanguageEnumCollection();

            Assert.NotNull(collection);
            Assert.Empty(collection);
        }

        [Fact]
        [DisplayName("LanguageItemCollection 無參建構子應建立非 null 的空集合")]
        public void LanguageItemCollection_ParameterlessConstructor_CreatesEmptyCollection()
        {
            var collection = new LanguageItemCollection();

            Assert.NotNull(collection);
            Assert.Empty(collection);
        }

        [Fact]
        [DisplayName("LanguageEnumEntryCollection 無參建構子應建立非 null 的空集合")]
        public void LanguageEnumEntryCollection_ParameterlessConstructor_CreatesEmptyCollection()
        {
            var collection = new LanguageEnumEntryCollection();

            Assert.NotNull(collection);
            Assert.Empty(collection);
        }
    }
}
