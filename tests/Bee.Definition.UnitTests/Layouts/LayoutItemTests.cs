using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutItem 單元測試。
    /// </summary>
    public class LayoutItemTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為對應預設值")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var item = new LayoutItem();

            Assert.Equal(string.Empty, item.FieldName);
            Assert.Equal(string.Empty, item.Caption);
            Assert.Equal(ControlType.TextEdit, item.ControlType);
            Assert.Equal(1, item.RowSpan);
            Assert.Equal(1, item.ColumnSpan);
            Assert.Equal(string.Empty, item.ProgId);
            Assert.False(item.ReadOnly);
            Assert.Equal(string.Empty, item.DisplayFormat);
            Assert.Equal(string.Empty, item.NumberFormat);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(-100, 1)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [DisplayName("RowSpan 小於 1 時應被修正為 1")]
        public void RowSpan_BelowOne_ClampedToOne(int input, int expected)
        {
            var item = new LayoutItem { RowSpan = input };

            Assert.Equal(expected, item.RowSpan);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(-100, 1)]
        [InlineData(1, 1)]
        [InlineData(3, 3)]
        [InlineData(10, 10)]
        [DisplayName("ColumnSpan 小於 1 時應被修正為 1")]
        public void ColumnSpan_BelowOne_ClampedToOne(int input, int expected)
        {
            var item = new LayoutItem { ColumnSpan = input };

            Assert.Equal(expected, item.ColumnSpan);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"FieldName - Caption\"")]
        public void ToString_ReturnsFormatted()
        {
            var item = new LayoutItem { FieldName = "Amount", Caption = "金額" };

            Assert.Equal("Amount - 金額", item.ToString());
        }

        [Fact]
        [DisplayName("ListItems 未序列化狀態應回傳集合實例")]
        public void ListItems_DefaultState_ReturnsCollection()
        {
            var item = new LayoutItem();

            Assert.NotNull(item.ListItems);
        }

        [Fact]
        [DisplayName("ListItems 於序列化且集合為空時應回傳 null")]
        public void ListItems_EmptyDuringSerialize_ReturnsNull()
        {
            var item = new LayoutItem();
            item.SetSerializeState(SerializeState.Serialize);

            Assert.Null(item.ListItems);
        }

        [Fact]
        [DisplayName("ExtendedProperties 未序列化狀態應回傳集合實例")]
        public void ExtendedProperties_DefaultState_ReturnsCollection()
        {
            var item = new LayoutItem();

            Assert.NotNull(item.ExtendedProperties);
        }

        [Fact]
        [DisplayName("ExtendedProperties 於序列化且集合為空時應回傳 null")]
        public void ExtendedProperties_EmptyDuringSerialize_ReturnsNull()
        {
            var item = new LayoutItem();
            item.SetSerializeState(SerializeState.Serialize);

            Assert.Null(item.ExtendedProperties);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var item = new LayoutItem();

            item.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, item.SerializeState);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var item = new LayoutItem
            {
                FieldName = "Amount",
                Caption = "金額",
                ControlType = ControlType.CheckEdit,
                RowSpan = 2,
                ColumnSpan = 3,
                ProgId = "P1",
                ReadOnly = true,
                DisplayFormat = "{0:C}",
                NumberFormat = "N2"
            };

            Assert.Equal("Amount", item.FieldName);
            Assert.Equal("金額", item.Caption);
            Assert.Equal(ControlType.CheckEdit, item.ControlType);
            Assert.Equal(2, item.RowSpan);
            Assert.Equal(3, item.ColumnSpan);
            Assert.Equal("P1", item.ProgId);
            Assert.True(item.ReadOnly);
            Assert.Equal("{0:C}", item.DisplayFormat);
            Assert.Equal("N2", item.NumberFormat);
        }
    }
}
