using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutColumn 單元測試。
    /// </summary>
    public class LayoutColumnTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為對應預設值")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var column = new LayoutColumn();

            Assert.Equal(string.Empty, column.FieldName);
            Assert.Equal(string.Empty, column.Caption);
            Assert.Equal(ColumnControlType.TextEdit, column.ControlType);
            Assert.Equal(string.Empty, column.ProgId);
            Assert.True(column.Visible);
            Assert.False(column.ReadOnly);
            Assert.Equal(0, column.Width);
            Assert.Equal(string.Empty, column.DisplayFormat);
            Assert.Equal(string.Empty, column.NumberFormat);
        }

        [Fact]
        [DisplayName("帶參數建構子應設定 FieldName、Caption 與 ControlType")]
        public void ParameterizedConstructor_SetsProperties()
        {
            var column = new LayoutColumn("Amount", "金額", ColumnControlType.TextEdit);

            Assert.Equal("Amount", column.FieldName);
            Assert.Equal("金額", column.Caption);
            Assert.Equal(ColumnControlType.TextEdit, column.ControlType);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"FieldName - Caption\"")]
        public void ToString_ReturnsFormatted()
        {
            var column = new LayoutColumn("Amount", "金額", ColumnControlType.TextEdit);

            Assert.Equal("Amount - 金額", column.ToString());
        }

        [Fact]
        [DisplayName("ListItems 未序列化狀態應回傳集合實例")]
        public void ListItems_DefaultState_ReturnsCollection()
        {
            var column = new LayoutColumn();

            var items = column.ListItems;

            Assert.NotNull(items);
        }

        [Fact]
        [DisplayName("ListItems 於序列化且集合為空時應回傳 null")]
        public void ListItems_EmptyDuringSerialize_ReturnsNull()
        {
            var column = new LayoutColumn();
            column.SetSerializeState(SerializeState.Serialize);

            Assert.Null(column.ListItems);
        }

        [Fact]
        [DisplayName("ExtendedProperties 未序列化狀態應回傳集合實例")]
        public void ExtendedProperties_DefaultState_ReturnsCollection()
        {
            var column = new LayoutColumn();

            var props = column.ExtendedProperties;

            Assert.NotNull(props);
        }

        [Fact]
        [DisplayName("ExtendedProperties 於序列化且集合為空時應回傳 null")]
        public void ExtendedProperties_EmptyDuringSerialize_ReturnsNull()
        {
            var column = new LayoutColumn();
            column.SetSerializeState(SerializeState.Serialize);

            Assert.Null(column.ExtendedProperties);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var column = new LayoutColumn();

            column.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, column.SerializeState);
        }

        [Fact]
        [DisplayName("屬性應可被設定並讀回")]
        public void Properties_AreSettable()
        {
            var column = new LayoutColumn
            {
                FieldName = "A",
                Caption = "B",
                ControlType = ColumnControlType.CheckEdit,
                ProgId = "P1",
                Visible = false,
                ReadOnly = true,
                Width = 120,
                DisplayFormat = "{0:C}",
                NumberFormat = "N2"
            };

            Assert.Equal("A", column.FieldName);
            Assert.Equal("B", column.Caption);
            Assert.Equal(ColumnControlType.CheckEdit, column.ControlType);
            Assert.Equal("P1", column.ProgId);
            Assert.False(column.Visible);
            Assert.True(column.ReadOnly);
            Assert.Equal(120, column.Width);
            Assert.Equal("{0:C}", column.DisplayFormat);
            Assert.Equal("N2", column.NumberFormat);
        }
    }
}
