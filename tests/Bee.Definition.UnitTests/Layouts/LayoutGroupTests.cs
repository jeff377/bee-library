using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutGroup 單元測試。
    /// </summary>
    public class LayoutGroupTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為對應預設值")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var group = new LayoutGroup();

            Assert.Equal(string.Empty, group.Name);
            Assert.Equal(string.Empty, group.Caption);
            Assert.True(group.ShowCaption);
            Assert.Equal(1, group.ColumnCount);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"Name - Caption\"")]
        public void ToString_ReturnsFormatted()
        {
            var group = new LayoutGroup { Name = "G1", Caption = "主資料" };

            Assert.Equal("G1 - 主資料", group.ToString());
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(-100, 1)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [DisplayName("ColumnCount 小於 1 時應被修正為 1")]
        public void ColumnCount_BelowOne_ClampedToOne(int input, int expected)
        {
            var group = new LayoutGroup { ColumnCount = input };

            Assert.Equal(expected, group.ColumnCount);
        }

        [Fact]
        [DisplayName("Items 未序列化狀態應回傳集合實例")]
        public void Items_DefaultState_ReturnsCollection()
        {
            var group = new LayoutGroup();

            Assert.NotNull(group.Items);
        }

        [Fact]
        [DisplayName("Items 於序列化且集合為空時應回傳 null")]
        public void Items_EmptyDuringSerialize_ReturnsNull()
        {
            var group = new LayoutGroup();
            group.SetSerializeState(SerializeState.Serialize);

            Assert.Null(group.Items);
        }

        [Fact]
        [DisplayName("FindGrid 應依 TableName 找到對應的 LayoutGrid")]
        public void FindGrid_ExistingTableName_ReturnsGrid()
        {
            var group = new LayoutGroup();
            var grid = new LayoutGrid("Orders", "訂單");
            group.Items!.Add(grid);

            var found = group.FindGrid("Orders");

            Assert.NotNull(found);
            Assert.Same(grid, found);
        }

        [Fact]
        [DisplayName("FindGrid 於大小寫不同時仍能找到（使用 StrFunc.IsEquals）")]
        public void FindGrid_CaseInsensitiveMatch_ReturnsGrid()
        {
            var group = new LayoutGroup();
            group.Items!.Add(new LayoutGrid("Orders", "訂單"));

            var found = group.FindGrid("orders");

            Assert.NotNull(found);
        }

        [Fact]
        [DisplayName("FindGrid 於找不到時應回傳 null")]
        public void FindGrid_Missing_ReturnsNull()
        {
            var group = new LayoutGroup();
            group.Items!.Add(new LayoutGrid("Orders", "訂單"));

            var found = group.FindGrid("Customers");

            Assert.Null(found);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var group = new LayoutGroup();

            group.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, group.SerializeState);
        }
    }
}
