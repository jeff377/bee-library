using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Maui.Controls;

namespace Bee.UI.Maui.UnitTests.Controls
{
    /// <summary>
    /// 補強 <see cref="FormPage"/> 中尚未覆蓋的路徑：
    /// <c>ComputeSelectFields</c>（有欄位時的迴圈與去重邏輯）與
    /// <c>OnDeleteClickedAsync</c>（成功刪除 / 刪除失敗兩個分支）。
    /// </summary>
    [Collection("ClientInfo")]
    public class FormPageCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildSchemaWithListFields(string listFields)
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            schema.ListFields = listFields;
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("emp_id", "Employee ID", FieldDbType.String);
            master.Fields.Add("emp_name", "Name", FieldDbType.String);
            return schema;
        }

        private static DataSet BuildServerDataSet(Guid rowId, string name)
        {
            var ds = new DataSet(TestProgId);
            var master = new DataTable(TestProgId);
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add("emp_name", typeof(string));
            master.Rows.Add(rowId, name);
            ds.Tables.Add(master);
            ds.AcceptChanges();
            return ds;
        }

        private static DataTable BuildListTable(Guid rowId, string name)
        {
            var table = new DataTable(TestProgId);
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("emp_id", typeof(string));
            table.Columns.Add("emp_name", typeof(string));
            table.Rows.Add(rowId, "E001", name);
            return table;
        }

        [Fact]
        [DisplayName("ComputeSelectFields 在 ListFields 有值時會前置 sys_rowid 並去除重複欄位")]
        public async Task ComputeSelectFields_WithListFields_PrependsSysRowIdAndDeduplicates()
        {
            // schema.ListFields 含重複的 sys_rowid 與 emp_id，應去重且 sys_rowid 排最前
            var schema = BuildSchemaWithListFields("emp_id,emp_name,sys_rowid,emp_id");
            string? capturedFields = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = fields =>
                {
                    capturedFields = fields;
                    return new GetListResponse { Table = new DataTable() };
                },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };

            await page.InitializeAsync();

            Assert.Equal("sys_rowid,emp_id,emp_name", capturedFields);
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 成功時呼叫 connector.DeleteAsync 並重設 DataObject")]
        public async Task OnDeleteClickedAsync_Success_CallsDeleteAndResetsDataObject()
        {
            var schema = BuildSchemaWithListFields("emp_id,emp_name");
            var rowId = Guid.NewGuid();
            Guid? deletedId = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(rowId, "Alice") },
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Alice") },
                DeleteHandler = id =>
                {
                    deletedId = id;
                    return new DeleteResponse { RowsAffected = 1 };
                },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var rowSelectMethod = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(rowSelectMethod);
            await (Task)rowSelectMethod!.Invoke(page, new object[] { rowId })!;
            Assert.NotNull(page.DataObject!.MasterRow);

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(deleteMethod);
            await (Task)deleteMethod!.Invoke(page, Array.Empty<object>())!;

            Assert.Equal(rowId, deletedId);
            Assert.Null(page.DataObject.MasterRow);
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 失敗時 ErrorOccurred 帶出例外訊息")]
        public async Task OnDeleteClickedAsync_Throws_FiresErrorOccurred()
        {
            var schema = BuildSchemaWithListFields("emp_id");
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(rowId, "Bob") },
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Bob") },
                DeleteHandler = _ => throw new InvalidOperationException("delete refused"),
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var rowSelectMethod = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)rowSelectMethod!.Invoke(page, new object[] { rowId })!;

            Exception? captured = null;
            page.ErrorOccurred += (_, ex) => captured = ex;

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)deleteMethod!.Invoke(page, Array.Empty<object>())!;

            Assert.NotNull(captured);
            Assert.Equal("delete refused", captured!.Message);
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<string, GetListResponse>? GetListHandler { get; set; }
            public Func<Guid, GetDataResponse>? GetDataHandler { get; set; }
            public Func<DataSet, SaveResponse>? SaveHandler { get; set; }
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (_ => new GetListResponse()))(selectFields));

            public override Task<GetDataResponse> GetDataAsync(Guid rowId)
                => Task.FromResult((GetDataHandler ?? (_ => new GetDataResponse()))(rowId));

            public override Task<SaveResponse> SaveAsync(DataSet dataSet)
                => Task.FromResult((SaveHandler ?? (_ => new SaveResponse()))(dataSet));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }
    }
}
