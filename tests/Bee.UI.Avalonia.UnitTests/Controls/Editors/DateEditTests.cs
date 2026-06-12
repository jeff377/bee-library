using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Behaviour checks for <see cref="DateEdit"/> / <see cref="YearMonthEdit"/>:
    /// ISO round-trip and the year-month variant.
    /// </summary>
    public class DateEditTests
    {
        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("hire_date", "Hire Date", FieldDbType.Date);
            master.Fields.Add("pay_month", "Pay Month", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        [Fact]
        [DisplayName("Bind 後載入日期初值")]
        public void Bind_ExistingDate_LoadsIntoSelectedDate()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("hire_date", "2026-06-11");

            var editor = new DateEdit();
            editor.Bind(dataObject, "hire_date");

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Unspecified), editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("選取日期以 yyyy-MM-dd 寫回")]
        public void SelectedDateChanged_AfterBind_WritesBackIsoDate()
        {
            var dataObject = BuildDataObject();
            var editor = new DateEdit();
            editor.Bind(dataObject, "hire_date");

            editor.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            Assert.Equal("2026-01-15", dataObject.GetField("hire_date"));
        }

        [Fact]
        [DisplayName("YearMonthEdit 隱藏日欄並以 yyyy-MM 寫回")]
        public void YearMonthEdit_SelectedDate_WritesBackYearMonth()
        {
            var dataObject = BuildDataObject();
            var editor = new YearMonthEdit();
            Assert.False(editor.DayVisible);

            editor.Bind(dataObject, "pay_month");
            editor.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            Assert.Equal("2026-03", dataObject.GetField("pay_month"));
        }

        [Fact]
        [DisplayName("空欄位 Bind 後 SelectedDate 為 null")]
        public void Bind_EmptyValue_LeavesSelectedDateNull()
        {
            var dataObject = BuildDataObject();
            var editor = new DateEdit();

            editor.Bind(dataObject, "pay_month");

            Assert.Null(editor.SelectedDate);
        }

        [Fact]
        [DisplayName("AllowEditModes=Add 時僅新增模式啟用")]
        public void SetControlState_AllowEditModesAdd_OnlyAddEnabled()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "hire_date", AllowEditModes = FormEditModes.Add };
            var editor = new DateEdit();
            editor.Bind(dataObject, field);

            editor.SetControlState(SingleFormMode.Add);
            Assert.True(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.Edit);
            Assert.False(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.View);
            Assert.False(editor.IsEnabled);
        }
    }
}
