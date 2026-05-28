using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.Components;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Server.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="DynamicGrid.OnRowClickAsync"/> 私有方法的三條路徑：
    /// 無委派直接返回、委派已設但無 RowId、委派已設且成功觸發；
    /// 額外補強 <c>FormatCell</c> 的 <c>_ =&gt;</c> fallback 分支（非 IFormattable 值）。
    /// </summary>
    public class DynamicGridOnRowClickTests
    {
        private static async Task InvokeOnRowClickAsync(DynamicGrid component, DataRow row)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "OnRowClickAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(component, new object[] { row })!;
            await task;
        }

        private static string InvokeFormatCell(DataRow row, LayoutColumn column)
        {
            var method = typeof(DynamicGrid).GetMethod(
                "FormatCell", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method!.Invoke(null, new object[] { row, column })!;
        }

        [Fact]
        [DisplayName("OnRowClickAsync OnRowSelected 未設定委派時應直接返回，不拋例外")]
        public async Task OnRowClickAsync_NoDelegate_DoesNotThrow()
        {
            var component = new DynamicGrid();
            var table = new DataTable();
            table.Columns.Add("col", typeof(string));
            var row = table.NewRow();
            table.Rows.Add(row);
            var exception = await Record.ExceptionAsync(() => InvokeOnRowClickAsync(component, row));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("OnRowClickAsync 委派已設定但資料列無 sys_rowid 欄位時，不應觸發委派")]
        public async Task OnRowClickAsync_DelegateSet_RowWithoutRowId_DoesNotInvokeCallback()
        {
            var component = new DynamicGrid();
            var invoked = false;
            Action<Guid> handler = _ => { invoked = true; };
            var callback = new EventCallback<Guid>(null, handler);
            typeof(DynamicGrid)
                .GetProperty("OnRowSelected", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, callback);
            var table = new DataTable();
            table.Columns.Add("other", typeof(string));
            var row = table.NewRow();
            table.Rows.Add(row);
            await InvokeOnRowClickAsync(component, row);
            Assert.False(invoked);
        }

        [Fact]
        [DisplayName("OnRowClickAsync 委派已設定且資料列含有效 sys_rowid 時，應觸發委派並傳入正確 Guid")]
        public async Task OnRowClickAsync_DelegateSet_ValidRowId_InvokesCallbackWithExpectedGuid()
        {
            var expected = Guid.NewGuid();
            var component = new DynamicGrid();
            var receivedId = Guid.Empty;
            Action<Guid> handler = id => { receivedId = id; };
            var callback = new EventCallback<Guid>(null, handler);
            typeof(DynamicGrid)
                .GetProperty("OnRowSelected", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(component, callback);
            var table = new DataTable();
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            var row = table.NewRow();
            row[SysFields.RowId] = expected;
            table.Rows.Add(row);
            await InvokeOnRowClickAsync(component, row);
            Assert.Equal(expected, receivedId);
        }

        [Fact]
        [DisplayName("FormatCell 欄位值為非 IFormattable 型別時應回傳 ToString() 結果")]
        public void FormatCell_NonFormattableValue_ReturnsToStringResult()
        {
            var table = new DataTable();
            table.Columns.Add("data", typeof(object));
            var bytes = new byte[] { 1, 2, 3 };
            var row = table.NewRow();
            row["data"] = bytes;
            table.Rows.Add(row);
            var column = new LayoutColumn { FieldName = "data" };
            var result = InvokeFormatCell(row, column);
            Assert.Equal(bytes.ToString()!, result);
        }
    }
}
