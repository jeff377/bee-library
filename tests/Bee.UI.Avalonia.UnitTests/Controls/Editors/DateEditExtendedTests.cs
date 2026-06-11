using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="DateEdit"/> 的覆蓋率：ParseToOffset、SetControlState、FieldValue setter、
    /// LayoutField 繫結、Unbind、StyleKeyOverride。
    /// </summary>
    public class DateEditExtendedTests
    {
        private static DateTimeOffset? InvokeParseToOffset(string? raw)
        {
            var method = typeof(DateEdit).GetMethod(
                "ParseToOffset", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (DateTimeOffset?)method!.Invoke(null, new object?[] { raw });
        }

        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("hire_date", "Hire Date", FieldDbType.Date);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        [Fact]
        [DisplayName("ParseToOffset null 輸入回傳 null")]
        public void ParseToOffset_NullInput_ReturnsNull()
        {
            var result = InvokeParseToOffset(null);

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("ParseToOffset 無效字串回傳 null")]
        public void ParseToOffset_InvalidString_ReturnsNull()
        {
            var result = InvokeParseToOffset("not-a-date");

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("ParseToOffset 帶時間字串只取日期部分並以 Offset 零回傳")]
        public void ParseToOffset_DateWithTime_StripsTimeAndZeroOffset()
        {
            var result = InvokeParseToOffset("2026-01-15 14:30:00");

            Assert.NotNull(result);
            Assert.Equal(new DateTime(2026, 1, 15), result!.Value.DateTime);
            Assert.Equal(TimeSpan.Zero, result.Value.Offset);
        }

        [Fact]
        [DisplayName("SetControlState View 模式停用編輯器")]
        public void SetControlState_ViewMode_DisablesEditor()
        {
            var editor = new DateEdit();

            editor.SetControlState(SingleFormMode.View);

            Assert.False(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("SetControlState Edit 模式啟用編輯器")]
        public void SetControlState_EditMode_EnablesEditor()
        {
            var editor = new DateEdit();
            editor.SetControlState(SingleFormMode.View);

            editor.SetControlState(SingleFormMode.Edit);

            Assert.True(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("FieldValue 設為 null 清除 SelectedDate")]
        public void FieldValue_SetNull_ClearsSelectedDate()
        {
            var editor = new DateEdit
            {
                SelectedDate = new DateTimeOffset(
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
            };

            editor.FieldValue = null;

            Assert.Null(editor.SelectedDate);
        }

        [Fact]
        [DisplayName("FieldValue 設為 DateTime 轉換為 Unspecified-Kind DateTimeOffset 並只取日期")]
        public void FieldValue_SetWithDateTime_ConvertsToUnspecifiedOffset()
        {
            var editor = new DateEdit();

            editor.FieldValue = new DateTime(2026, 3, 15, 10, 20, 30, DateTimeKind.Utc);

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(new DateTime(2026, 3, 15), editor.SelectedDate!.Value.DateTime);
            Assert.Equal(TimeSpan.Zero, editor.SelectedDate.Value.Offset);
        }

        [Fact]
        [DisplayName("FieldValue 設為 DateTimeOffset 直接套用")]
        public void FieldValue_SetWithDateTimeOffset_UsesDirectly()
        {
            var expected = new DateTimeOffset(
                new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);
            var editor = new DateEdit();

            editor.FieldValue = expected;

            Assert.Equal(expected, editor.SelectedDate);
        }

        [Fact]
        [DisplayName("FieldValue 設為日期字串時解析為 DateTimeOffset")]
        public void FieldValue_SetWithString_ParsesDateString()
        {
            var editor = new DateEdit();

            editor.FieldValue = "2026-06-15";

            Assert.NotNull(editor.SelectedDate);
            Assert.Equal(new DateTime(2026, 6, 15), editor.SelectedDate!.Value.DateTime);
        }

        [Fact]
        [DisplayName("FieldValue getter 回傳 SelectedDate")]
        public void FieldValue_Getter_ReturnsSelectedDate()
        {
            var expected = new DateTimeOffset(
                new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);
            var editor = new DateEdit { SelectedDate = expected };

            Assert.Equal(expected, editor.FieldValue);
        }

        [Fact]
        [DisplayName("Bind 帶唯讀 LayoutField 停用編輯器並載入初值")]
        public void Bind_WithReadOnlyLayoutField_DisablesEditorAndLoadsValue()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("hire_date", "2026-01-01");
            var field = new LayoutField { FieldName = "hire_date", ReadOnly = true };
            var editor = new DateEdit();

            editor.Bind(dataObject, field);

            Assert.False(editor.IsEnabled);
            Assert.NotNull(editor.SelectedDate);
        }

        [Fact]
        [DisplayName("Unbind 後日期變更不寫回資料物件")]
        public void Unbind_AfterBind_StopsWriteBack()
        {
            var dataObject = BuildDataObject();
            var editor = new DateEdit();
            editor.Bind(dataObject, "hire_date");
            editor.Unbind();

            editor.SelectedDate = new DateTimeOffset(
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

            Assert.Equal(string.Empty, dataObject.GetField("hire_date"));
        }

        [Fact]
        [DisplayName("StyleKeyOverride 回傳 DatePicker 類型（確保控制項能套用主題）")]
        public void StyleKeyOverride_ReturnsDatePickerType()
        {
            var editor = new DateEdit();

            var styleKey = typeof(global::Avalonia.StyledElement)
                .GetProperty("StyleKeyOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editor);

            Assert.Equal(typeof(global::Avalonia.Controls.DatePicker), styleKey);
        }
    }
}
