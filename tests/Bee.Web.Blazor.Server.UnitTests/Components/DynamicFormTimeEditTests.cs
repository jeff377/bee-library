using System.ComponentModel;
using System.Reflection;
using AngleSharp.Dom;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;
using Bee.Web.Blazor.Server.DataObjects;
using Bunit;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// DynamicForm 時刻輸入（<see cref="ControlType.TimeEdit"/>）的行為測試，對齊 Avalonia 的
    /// <c>TimeEdit</c>：寬鬆輸入正規化為定寬 HH:mm、清空代表未填、無法解析的輸入保留前一個有效值。
    /// </summary>
    public class DynamicFormTimeEditTests : BunitContext
    {
        private const string FieldName = "work_start";
        private const string TimeInputSelector = "input.bee-dynamic-form__input--time";

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Shift", "Shift");
            schema.Tables!.Add("Shift", "Shift").Fields!.Add(FieldName, "Start", FieldDbType.Time);
            return schema;
        }

        private IRenderedComponent<DynamicForm> RenderForm(FormSchema schema, FormDataObject dataObject)
            => Render<DynamicForm>(p => p
                .Add(c => c.Layout, FormLayoutGenerator.Generate(schema, "default"))
                .Add(c => c.DataObject, dataObject));

        private static FormDataObject BuildDataObject(FormSchema schema)
        {
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static IElement TimeInput(IRenderedComponent<DynamicForm> cut) => cut.Find(TimeInputSelector);

        [Fact]
        [DisplayName("渲染時以定寬 HH:mm 顯示")]
        public void Render_DisplaysFixedWidthForm()
        {
            var schema = BuildSchema();
            var dataObject = BuildDataObject(schema);
            // Assigned to the row directly: `SetField` already normalises, which would let this pass
            // without the component doing any normalising of its own.
            dataObject.MasterRow![FieldName] = "8:30";

            var cut = RenderForm(schema, dataObject);

            Assert.Equal("08:30", TimeInput(cut).GetAttribute("value"));
        }

        [Fact]
        [DisplayName("變更時應將寬鬆輸入正規化為定寬 HH:mm")]
        public void Change_NormalizesLooseInput()
        {
            var schema = BuildSchema();
            var dataObject = BuildDataObject(schema);
            var cut = RenderForm(schema, dataObject);

            TimeInput(cut).Change("8:30");

            Assert.Equal("08:30", dataObject.GetField(FieldName));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("清空輸入框應寫回空字串（未填），不是 00:00")]
        public void Change_EmptyText_WritesUnset(string empty)
        {
            var schema = BuildSchema();
            var dataObject = BuildDataObject(schema);
            dataObject.SetField(FieldName, "08:30");
            var cut = RenderForm(schema, dataObject);

            TimeInput(cut).Change(empty);

            Assert.Equal(string.Empty, dataObject.GetField(FieldName));
        }

        [Fact]
        [DisplayName("00:00 為合法時刻，應正常寫回")]
        public void Change_Midnight_IsStored()
        {
            var schema = BuildSchema();
            var dataObject = BuildDataObject(schema);
            var cut = RenderForm(schema, dataObject);

            TimeInput(cut).Change("00:00");

            Assert.Equal("00:00", dataObject.GetField(FieldName));
        }

        [Theory]
        [InlineData("25:00")]
        [InlineData("08:99")]
        [InlineData("abc")]
        [DisplayName("無法解析的輸入應保留前一個有效值，不清空欄位")]
        public void Change_InvalidText_KeepsLastValidValue(string invalid)
        {
            var schema = BuildSchema();
            var dataObject = BuildDataObject(schema);
            dataObject.SetField(FieldName, "08:30");
            var cut = RenderForm(schema, dataObject);

            TimeInput(cut).Change(invalid);

            Assert.Equal("08:30", dataObject.GetField(FieldName));
        }

        [Fact]
        [DisplayName("時刻輸入的變更事件應標記回推 value，被拒絕的輸入才會在瀏覽器上回到保留值")]
        public void ChangeHandler_UpdatesValueAttribute()
        {
            var schema = BuildSchema();
            var dataObject = BuildDataObject(schema);
            var cut = RenderForm(schema, dataObject);

            // A rejected input leaves the model unchanged, so the re-render emits the same `value` as
            // before and the browser would keep showing the typo. The renderer only pushes the kept value
            // back when the change handler is marked as updating `value`, because it then records the
            // typed text before diffing. That marker is asserted directly: bUnit rebuilds its markup from
            // the render tree on every render, so a DOM assertion passes with or without it.
            var changeHandlers = CurrentFrames(cut)
                .Where(f => FrameProperty(f, "FrameType")!.ToString() == "Attribute"
                    && (string?)FrameProperty(f, "AttributeName") == "onchange")
                .ToList();

            var handler = Assert.Single(changeHandlers);
            Assert.Equal("value", FrameProperty(handler, "AttributeEventUpdatesAttributeName"));
        }

        // Reflection keeps the render tree types out of the source: `GetCurrentRenderTreeFrames` is
        // protected on the renderer, and naming the frame types directly is rejected by BL0006.
        private List<object> CurrentFrames(IRenderedComponent<DynamicForm> cut)
        {
            var method = Renderer.GetType().GetMethod("GetCurrentRenderTreeFrames",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var range = method!.Invoke(Renderer, [cut.ComponentId])!;
            var array = (Array)range.GetType().GetField("Array")!.GetValue(range)!;
            var count = (int)range.GetType().GetField("Count")!.GetValue(range)!;
            return Enumerable.Range(0, count).Select(i => array.GetValue(i)!).ToList();
        }

        private static object? FrameProperty(object frame, string name)
            => frame.GetType().GetProperty(name)!.GetValue(frame);
    }
}
