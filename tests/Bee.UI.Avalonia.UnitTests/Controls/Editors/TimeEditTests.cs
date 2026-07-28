using System.ComponentModel;
using Avalonia.Input;
using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Behaviour checks for <see cref="TimeEdit"/>: commit-time normalisation to the fixed-width
    /// <c>"HH:mm"</c> storage form, an explicit empty meaning "unset", and tolerance of input that
    /// does not parse.
    /// </summary>
    public class TimeEditTests
    {
        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Shift", "Shift");
            var master = schema.Tables!.Add("Shift", "Shift");
            master.Fields!.Add("work_start", "Start", FieldDbType.Time);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static LayoutField StartField() => new() { FieldName = "work_start" };

        // Commit is exercised via Enter (a KeyDown routed event), matching NumericEditTests; the
        // LostFocus routed event carries a FocusChangedEventArgs that cannot be synthesised here.
        private static void Commit(TimeEdit editor)
            => editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

        [Fact]
        [DisplayName("Bind 後以定寬 HH:mm 顯示")]
        public void Bind_DisplaysFixedWidthForm()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("work_start", "8:30");

            var editor = new TimeEdit();
            editor.Bind(dataObject, StartField());

            Assert.Equal("08:30", editor.Text);
        }

        [Fact]
        [DisplayName("提交時應將寬鬆輸入正規化為定寬 HH:mm")]
        public void Commit_NormalizesLooseInput()
        {
            var dataObject = BuildDataObject();
            var editor = new TimeEdit();
            editor.Bind(dataObject, StartField());

            editor.Text = "8:30";
            Commit(editor);

            // Fixed width is what makes the stored value sort chronologically.
            Assert.Equal("08:30", dataObject.GetField("work_start"));
        }

        [Fact]
        [DisplayName("清空欄位應寫回空字串（未填），不是 00:00")]
        public void Commit_EmptyText_WritesUnset()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("work_start", "08:30");
            var editor = new TimeEdit();
            editor.Bind(dataObject, StartField());

            editor.Text = string.Empty;
            Commit(editor);

            // Midnight is a legal value, so it cannot double as "unset".
            Assert.Equal(string.Empty, dataObject.GetField("work_start"));
        }

        [Fact]
        [DisplayName("00:00 為合法時刻，應正常寫回")]
        public void Commit_Midnight_IsStored()
        {
            var dataObject = BuildDataObject();
            var editor = new TimeEdit();
            editor.Bind(dataObject, StartField());

            editor.Text = "00:00";
            Commit(editor);

            Assert.Equal("00:00", dataObject.GetField("work_start"));
        }

        [Theory]
        [InlineData("25:00")]
        [InlineData("08:99")]
        [InlineData("abc")]
        [DisplayName("無法解析的輸入應保留前一個有效值，不清空欄位")]
        public void Commit_InvalidText_KeepsLastValidValue(string invalid)
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("work_start", "08:30");
            var editor = new TimeEdit();
            editor.Bind(dataObject, StartField());

            editor.Text = invalid;
            Commit(editor);

            Assert.Equal("08:30", dataObject.GetField("work_start"));
        }

        [Fact]
        [DisplayName("輸入長度上限應為時刻格式寬度")]
        public void MaxLength_MatchesStorageWidth()
        {
            Assert.Equal(ValueUtilities.TimeOnlyLength, new TimeEdit().MaxLength);
        }

        [Fact]
        [DisplayName("編輯器工廠對 TimeEdit 應產生 TimeEdit 控件")]
        public void Factory_TimeEdit_CreatesTimeEditor()
        {
            Assert.IsType<TimeEdit>(FieldEditorFactory.Create(ControlType.TimeEdit));
        }
    }
}
