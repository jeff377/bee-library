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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-date")]
        [DisplayName("無效或空日期字串 Bind 後 SelectedDate 為 null")]
        public void Bind_InvalidOrEmptyDateString_LeavesSelectedDateNull(string? invalidDate)
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("hire_date", invalidDate ?? string.Empty);
            var editor = new DateEdit();

            editor.Bind(dataObject, "hire_date");

            Assert.Null(editor.SelectedDate);
        }

        [Fact]
        [DisplayName("FieldValue setter 為 null 時清空 SelectedDate")]
        public void FieldValue_Null_ClearsSelectedDate()
        {
            var editor = new DateEdit();
            editor.SelectedDate = new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            editor.FieldValue = null;

            Assert.Null(editor.SelectedDate);
        }

        [Fact]
        [DisplayName("FieldValue setter 接受 DateTimeOffset 直接設定 SelectedDate")]
        public void FieldValue_DateTimeOffset_SetsSelectedDate()
        {
            var editor = new DateEdit();
            var offset = new DateTimeOffset(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            editor.FieldValue = offset;

            Assert.Equal(offset, editor.SelectedDate);
        }

        [Fact]
        [DisplayName("FieldValue setter 接受 DateTime 並擷取日期部分（無時間）")]
        public void FieldValue_DateTime_ConvertsToDatePartOnly()
        {
            var editor = new DateEdit();
            var dt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);

            editor.FieldValue = dt;

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(new DateTime(2026, 3, 15), editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("FieldValue setter 接受字串並以 ParseToOffset 解析")]
        public void FieldValue_String_ParsesDateFromString()
        {
            var editor = new DateEdit();

            editor.FieldValue = "2026-09-20";

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(new DateTime(2026, 9, 20), editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("Unbind 後欄位變更不再更新 SelectedDate（保留解除綁定前的值）")]
        public void Unbind_AfterBind_StopsTrackingChanges()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("hire_date", "2026-01-15");
            var editor = new DateEdit();
            editor.Bind(dataObject, "hire_date");
            Assert.NotNull(editor.SelectedDate);

            editor.Unbind();
            dataObject.SetField("hire_date", "2026-12-01");

            Assert.Equal(new DateTime(2026, 1, 15), editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("ReadOnly Layout 欄位 Bind 後立即停用編輯器")]
        public void Bind_ReadOnlyLayoutField_DisablesEditor()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "hire_date", ReadOnly = true };
            var editor = new DateEdit();

            editor.Bind(dataObject, field);

            Assert.False(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("無 AllowEditModes 限制時 View 模式停用、Edit/Add 模式啟用")]
        public void SetControlState_NoAllowEditModesConstraint_ViewDisabledEditEnabled()
        {
            var dataObject = BuildDataObject();
            var editor = new DateEdit();
            editor.Bind(dataObject, "hire_date");

            editor.SetControlState(SingleFormMode.View);
            Assert.False(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.Edit);
            Assert.True(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.Add);
            Assert.True(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("BindRow 以 DataRow 為目標，SelectedDate 變更寫回明細列")]
        public void Bind_RowScoped_WritesBackToTargetRow()
        {
            var schema = new FormSchema("Order", "Order");
            var master = schema.Tables!.Add("Order", "Order");
            master.Fields!.Add("order_no", "Order No", FieldDbType.String);
            var detail = schema.Tables.Add("OrderLine", "OrderLine");
            detail.Fields!.Add("due_date", "Due Date", FieldDbType.Date);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var lineTable = dataObject.DataSet.Tables["OrderLine"]!;
            var row = lineTable.NewRow();
            lineTable.Rows.Add(row);

            var column = new LayoutColumn("due_date", "Due Date", ControlType.DateEdit);
            var editor = new DateEdit();
            editor.Bind(dataObject, column, row);

            editor.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            Assert.Equal("2026-07-31", dataObject.GetField(row, "due_date"));
        }
    }
}
