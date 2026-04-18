using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutGrid 單元測試。
    /// </summary>
    public class LayoutGridTests
    {
        [Fact]
        [DisplayName("預設建構子應初始化為對應預設值")]
        public void DefaultConstructor_InitializesDefaults()
        {
            var grid = new LayoutGrid();

            Assert.Equal(string.Empty, grid.TableName);
            Assert.Equal(string.Empty, grid.DisplayName);
            Assert.Equal(GridControlAllowActions.All, grid.AllowActions);
        }

        [Fact]
        [DisplayName("帶參數建構子應設定 TableName 與 DisplayName")]
        public void ParameterizedConstructor_SetsProperties()
        {
            var grid = new LayoutGrid("Orders", "訂單");

            Assert.Equal("Orders", grid.TableName);
            Assert.Equal("訂單", grid.DisplayName);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"TableName - DisplayName\"")]
        public void ToString_ReturnsFormatted()
        {
            var grid = new LayoutGrid("Orders", "訂單");

            Assert.Equal("Orders - 訂單", grid.ToString());
        }

        [Fact]
        [DisplayName("Columns 未序列化狀態應回傳集合實例")]
        public void Columns_DefaultState_ReturnsCollection()
        {
            var grid = new LayoutGrid();

            Assert.NotNull(grid.Columns);
        }

        [Fact]
        [DisplayName("Columns 於序列化且集合為空時應回傳 null")]
        public void Columns_EmptyDuringSerialize_ReturnsNull()
        {
            var grid = new LayoutGrid();
            grid.SetSerializeState(SerializeState.Serialize);

            Assert.Null(grid.Columns);
        }

        [Fact]
        [DisplayName("SetSerializeState 應設定自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var grid = new LayoutGrid();

            grid.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, grid.SerializeState);
        }

        [Fact]
        [DisplayName("AllowActions 屬性應可被設定並讀回")]
        public void AllowActions_Settable()
        {
            var grid = new LayoutGrid { AllowActions = GridControlAllowActions.None };

            Assert.Equal(GridControlAllowActions.None, grid.AllowActions);
        }
    }
}
