using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutField 單元測試。
    /// </summary>
    public class LayoutFieldTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為對應預設值")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var field = new LayoutField();

            Assert.Equal(string.Empty, field.FieldName);
            Assert.Equal(string.Empty, field.Caption);
            Assert.Equal(ControlType.TextEdit, field.ControlType);
            Assert.Equal(1, field.RowSpan);
            Assert.Equal(1, field.ColumnSpan);
            Assert.True(field.Visible);
            Assert.False(field.ReadOnly);
            Assert.Equal(string.Empty, field.DisplayFormat);
            Assert.Equal(string.Empty, field.NumberFormat);
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
            var field = new LayoutField { RowSpan = input };

            Assert.Equal(expected, field.RowSpan);
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
            var field = new LayoutField { ColumnSpan = input };

            Assert.Equal(expected, field.ColumnSpan);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"FieldName - Caption\"")]
        public void ToString_ReturnsFormatted()
        {
            var field = new LayoutField { FieldName = "Amount", Caption = "金額" };

            Assert.Equal("Amount - 金額", field.ToString());
        }

        [Fact]
        [DisplayName("ExtendedProperties 未序列化狀態應回傳集合實例")]
        public void ExtendedProperties_DefaultState_ReturnsCollection()
        {
            var field = new LayoutField();

            Assert.NotNull(field.ExtendedProperties);
        }

        [Fact]
        [DisplayName("ExtendedProperties 於序列化且集合為空時應回傳 null")]
        public void ExtendedProperties_EmptyDuringSerialize_ReturnsNull()
        {
            var field = new LayoutField();
            field.SetSerializeState(SerializeState.Serialize);

            Assert.Null(field.ExtendedProperties);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var field = new LayoutField();

            field.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, field.SerializeState);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var field = new LayoutField
            {
                FieldName = "Amount",
                Caption = "金額",
                ControlType = ControlType.CheckEdit,
                RowSpan = 2,
                ColumnSpan = 3,
                Visible = false,
                ReadOnly = true,
                DisplayFormat = "{0:C}",
                NumberFormat = "N2"
            };

            Assert.Equal("Amount", field.FieldName);
            Assert.Equal("金額", field.Caption);
            Assert.Equal(ControlType.CheckEdit, field.ControlType);
            Assert.Equal(2, field.RowSpan);
            Assert.Equal(3, field.ColumnSpan);
            Assert.False(field.Visible);
            Assert.True(field.ReadOnly);
            Assert.Equal("{0:C}", field.DisplayFormat);
            Assert.Equal("N2", field.NumberFormat);
        }
    }
}
