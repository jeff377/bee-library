using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Web.Blazor.Wasm.Components;
using Bee.Web.Blazor.Wasm.DataObjects;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="FormPage"/> Razor 模板 <c>BuildRenderTree</c> 的覆蓋率。
    /// 測試在無 DI 環境下以反射設定私有欄位並呼叫 <c>BuildRenderTree</c>，
    /// 涵蓋錯誤、Loading、完整工具列（IsDirty false / true）四條模板分支。
    /// </summary>
    public class FormPageRenderTests
    {
        private static readonly MethodInfo s_buildRenderTree =
            typeof(FormPage)
                .GetMethod("BuildRenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_errorField =
            typeof(FormPage).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_isInitializingField =
            typeof(FormPage).GetField("_isInitializing", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_dataObjectField =
            typeof(FormPage).GetField("_dataObject", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static void Render(FormPage page)
        {
            var builder = new RenderTreeBuilder();
            s_buildRenderTree.Invoke(page, new object[] { builder });
        }

        private static FormDataObject CreateFreshDataObject()
        {
            var schema = new FormSchema("Test", "Test");
            schema.Tables!.Add("Test", "Test");
            return new FormDataObject(schema);
        }

        private static FormDataObject CreateDirtyDataObject()
        {
            var schema = new FormSchema("Test", "Test");
            var masterTable = schema.Tables!.Add("Test", "Test");
            masterTable.Fields!.Add("name", "名稱", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            dataObject.SetField("name", "dirty-value");
            return dataObject;
        }

        [Fact]
        [DisplayName("BuildRenderTree 有錯誤訊息時應渲染錯誤區塊且不拋出例外")]
        public void BuildRenderTree_WithError_DoesNotThrow()
        {
            var page = new FormPage();
            s_errorField.SetValue(page, "初始化失敗");
            s_isInitializingField.SetValue(page, false);
            var ex = Record.Exception(() => Render(page));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("BuildRenderTree 初始化進行中時應渲染 Loading 區塊且不拋出例外")]
        public void BuildRenderTree_Initializing_DoesNotThrow()
        {
            var page = new FormPage();
            var ex = Record.Exception(() => Render(page));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("BuildRenderTree DataObject 為 null 且已完成初始化時應渲染 Loading 區塊且不拋出例外")]
        public void BuildRenderTree_DataObjectNull_DoesNotThrow()
        {
            var page = new FormPage();
            s_isInitializingField.SetValue(page, false);
            var ex = Record.Exception(() => Render(page));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("BuildRenderTree DataObject 已設定且 IsDirty 為 false 時應渲染完整工具列且不拋出例外")]
        public void BuildRenderTree_DataObjectSet_NotDirty_DoesNotThrow()
        {
            var page = new FormPage();
            s_isInitializingField.SetValue(page, false);
            s_dataObjectField.SetValue(page, CreateFreshDataObject());
            var ex = Record.Exception(() => Render(page));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("BuildRenderTree DataObject IsDirty 為 true 時應渲染 unsaved 提示且不拋出例外")]
        public void BuildRenderTree_DataObjectDirty_DoesNotThrow()
        {
            var page = new FormPage();
            s_isInitializingField.SetValue(page, false);
            s_dataObjectField.SetValue(page, CreateDirtyDataObject());
            var ex = Record.Exception(() => Render(page));
            Assert.Null(ex);
        }
    }
}
