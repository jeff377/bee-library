using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutSection 單元測試。
    /// </summary>
    public class LayoutSectionTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為對應預設值")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var section = new LayoutSection();

            Assert.Equal(string.Empty, section.Name);
            Assert.Equal(string.Empty, section.Caption);
            Assert.True(section.ShowCaption);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"Name - Caption\"")]
        public void ToString_ReturnsFormatted()
        {
            var section = new LayoutSection { Name = "Main", Caption = "主資料" };

            Assert.Equal("Main - 主資料", section.ToString());
        }

        [Fact]
        [DisplayName("Fields 未序列化狀態應回傳集合實例")]
        public void Fields_DefaultState_ReturnsCollection()
        {
            var section = new LayoutSection();

            Assert.NotNull(section.Fields);
        }

        [Fact]
        [DisplayName("Fields 於序列化且集合為空時應回傳 null")]
        public void Fields_EmptyDuringSerialize_ReturnsNull()
        {
            var section = new LayoutSection();
            section.SetSerializeState(SerializeState.Serialize);

            Assert.Null(section.Fields);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var section = new LayoutSection();

            section.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, section.SerializeState);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var section = new LayoutSection
            {
                Name = "Main",
                Caption = "基本資料",
                ShowCaption = false
            };

            Assert.Equal("Main", section.Name);
            Assert.Equal("基本資料", section.Caption);
            Assert.False(section.ShowCaption);
        }
    }
}
