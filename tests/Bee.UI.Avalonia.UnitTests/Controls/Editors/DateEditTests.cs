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
        [DisplayName("View 模式停用編輯器")]
        public void SetControlState_ViewMode_DisablesEditor()
        {
            var editor = new DateEdit();

            editor.SetControlState(SingleFormMode.View);

            Assert.False(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("Edit 模式啟用編輯器")]
        public void SetControlState_EditMode_EnablesEditor()
        {
            var editor = new DateEdit();
            editor.SetControlState(SingleFormMode.View);

            editor.SetControlState(SingleFormMode.Edit);

            Assert.True(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("Bind(LayoutField) 載入欄位初值")]
        public void Bind_WithLayoutField_LoadsValue()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("hire_date", "2026-06-01");
            var field = new LayoutField { FieldName = "hire_date", Caption = "Hire Date" };
            var editor = new DateEdit();

            editor.Bind(dataObject, field);

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
                editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("ReadOnly layout field 下 Edit 模式仍停用編輯器")]
        public void Bind_WithReadOnlyLayoutField_StaysDisabledInEditMode()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "hire_date", Caption = "Hire Date", ReadOnly = true };
            var editor = new DateEdit();

            editor.Bind(dataObject, field);
            editor.SetControlState(SingleFormMode.Edit);

            Assert.False(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("Unbind 後欄位變更不再刷新編輯器")]
        public void Unbind_AfterBind_StopsTrackingFieldChanges()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("hire_date", "2026-06-11");
            var editor = new DateEdit();
            editor.Bind(dataObject, "hire_date");

            editor.Unbind();
            dataObject.SetField("hire_date", "2020-01-01");

            Assert.Equal(
                new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Unspecified),
                editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("FieldValue 設 null 清除 SelectedDate")]
        public void FieldValue_NullSetter_ClearsSelectedDate()
        {
            var editor = new DateEdit();
            editor.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            editor.FieldValue = null;

            Assert.Null(editor.SelectedDate);
        }

        [Fact]
        [DisplayName("FieldValue 設 DateTime 更新 SelectedDate")]
        public void FieldValue_DateTimeSetter_SetsSelectedDate()
        {
            var editor = new DateEdit();

            editor.FieldValue = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Unspecified);

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(new DateTime(2026, 5, 15), editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("Bind(row) 從指定明細列載入日期值並寫回該列")]
        public void Bind_WithDetailRow_LoadsAndWritesBackToTargetRow()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("start_date", "Start Date", FieldDbType.Date);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var phoneTable = dataObject.DataSet.Tables["EmployeePhone"]!;
            phoneTable.Rows.Add(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Unspecified));
            var targetRow = phoneTable.Rows[0];

            var field = new LayoutColumn("start_date", "Start Date", ControlType.DateEdit);
            var editor = new DateEdit();
            editor.Bind(dataObject, field, targetRow);

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(new DateTime(2026, 3, 15), editor.SelectedDate!.Value.DateTime);

            editor.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            Assert.Equal("2026-07-20", dataObject.GetField(targetRow, "start_date"));
        }
    }
}
