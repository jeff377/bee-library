using System.ComponentModel;
using Bee.Base.Serialization;
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
            Assert.Equal(string.Empty, grid.Caption);
            Assert.Equal(GridControlAllowActions.All, grid.AllowActions);
            Assert.Equal(FormEditModes.All, grid.AllowEditModes);
        }

        [Fact]
        [DisplayName("AllowEditModes 非預設值經 XML round-trip 還原；預設值不落檔")]
        public void AllowEditModes_XmlRoundTrip_PreservesValueAndOmitsDefault()
        {
            var layout = new FormLayout();
            var detail = new LayoutGrid("Orders", "訂單") { AllowEditModes = FormEditModes.Edit };
            detail.Columns!.Add(new LayoutColumn { FieldName = "qty", Caption = "Qty" });
            layout.Details!.Add(detail);
            layout.Details.Add(new LayoutGrid("Notes", "備註"));

            var xml = XmlCodec.Serialize(layout);
            Assert.Contains("AllowEditModes=\"Edit\"", xml);
            // The default (All) must not be written, so existing layout files stay untouched.
            Assert.Single(xml.Split("AllowEditModes", StringSplitOptions.None).Skip(1));

            var restored = XmlCodec.Deserialize<FormLayout>(xml);
            Assert.NotNull(restored);
            Assert.Equal(FormEditModes.Edit, restored!.Details![0].AllowEditModes);
            Assert.Equal(FormEditModes.All, restored.Details[1].AllowEditModes);
        }

        [Fact]
        [DisplayName("帶參數建構子應設定 TableName 與 Caption")]
        public void ParameterizedConstructor_SetsProperties()
        {
            var grid = new LayoutGrid("Orders", "訂單");

            Assert.Equal("Orders", grid.TableName);
            Assert.Equal("訂單", grid.Caption);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"TableName - Caption\"")]
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
