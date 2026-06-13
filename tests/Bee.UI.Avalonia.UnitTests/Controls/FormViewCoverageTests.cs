using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// 補強 <see cref="FormView"/> 覆蓋率：ComputeSelectFields 的 null schema 路徑、
    /// ListFields 組合（包含 sys_rowid 重複去重）、InitializeAsync 無輸入時的 no-op 路徑。
    /// </summary>
    public class FormViewCoverageTests
    {
        private static string InvokeComputeSelectFields(FormView view)
        {
            var method = typeof(FormView).GetMethod(
                "ComputeSelectFields", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (string)method!.Invoke(view, null)!;
        }

        [Fact]
        [DisplayName("ComputeSelectFields 在 Schema 為 null 時回傳空字串")]
        public void ComputeSelectFields_NullSchema_ReturnsEmpty()
        {
            var view = new TestFormView();

            var result = InvokeComputeSelectFields(view);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 在 ListFields 為 null 時僅回傳 sys_rowid")]
        public void ComputeSelectFields_NullListFields_ReturnsSysRowIdOnly()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            schema.ListFields = null!;

            var view = new TestFormView { Schema = schema };

            var result = InvokeComputeSelectFields(view);

            Assert.Equal(SysFields.RowId, result);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 在有 ListFields 時前置 sys_rowid 並包含全部欄位")]
        public void ComputeSelectFields_WithListFields_PrependsSysRowId()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_id", "ID", FieldDbType.String);
            master.Fields.Add("sys_name", "Name", FieldDbType.String);
            schema.ListFields = "sys_id,sys_name";

            var view = new TestFormView { Schema = schema };

            var result = InvokeComputeSelectFields(view);

            Assert.StartsWith(SysFields.RowId, result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sys_id", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sys_name", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 當 ListFields 已含 sys_rowid 時去重，不重複出現")]
        public void ComputeSelectFields_ListFieldsContainsSysRowId_Deduplicates()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_name", "Name", FieldDbType.String);
            schema.ListFields = $"{SysFields.RowId},sys_name";

            var view = new TestFormView { Schema = schema };

            var result = InvokeComputeSelectFields(view);

            // sys_rowid 應只出現一次
            var fields = result.Split(',');
            Assert.Single(fields, f => string.Equals(f, SysFields.RowId, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        [DisplayName("InitializeAsync 在無 ProgId 且 Schema/FormConnector 任一為 null 時為 no-op")]
        public async Task InitializeAsync_NoProgIdAndMissingSchema_IsNoOp()
        {
            // Schema 為 null，FormConnector 也 null → 直接回傳
            var view = new TestFormView();

            var exception = await Record.ExceptionAsync(view.InitializeAsync);

            Assert.Null(exception);
            Assert.Null(view.DataObject);
        }

        [Fact]
        [DisplayName("InitializeAsync 在無 ProgId 且只有 Schema 沒有 FormConnector 時為 no-op")]
        public async Task InitializeAsync_NoProgIdSchemaSetConnectorNull_IsNoOp()
        {
            var schema = new FormSchema("Employee", "Employee");
            schema.Tables!.Add("Employee", "Employee");
            var view = new TestFormView { Schema = schema };
            // FormConnector = null → guard: !hasProgId && (FormConnector is null) → return

            var exception = await Record.ExceptionAsync(view.InitializeAsync);

            Assert.Null(exception);
            Assert.Null(view.DataObject);
        }

        private sealed class TestFormView : FormView
        {
            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("Should not be reached in coverage tests.");

            protected override Guid ResolveAccessToken() => Guid.Empty;
        }
    }
}
