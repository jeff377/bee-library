using System.ComponentModel;
using System.Data;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.DataObjects
{
    /// <summary>
    /// 補強 <see cref="FormDataObject"/> 的測試覆蓋率：
    /// 含時間的 DateTime 格式、Guid 欄位解析，以及 Binary 欄位的 base64 解析。
    /// </summary>
    public class FormDataObjectCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("emp_id", "Employee ID", FieldDbType.String);
            master.Fields.Add("emp_name", "Name", FieldDbType.String);
            master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            return schema;
        }

        [Fact]
        [DisplayName("含時間部分的 DateTime 欄位以 yyyy-MM-ddTHH:mm:ss 格式回傳")]
        public void GetField_DateTimeWithTimePart_ReturnsIsoDateTime()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            dataObject.SetField("hire_date", "2026-05-21T14:30:00");

            Assert.Equal("2026-05-21T14:30:00", dataObject.GetField("hire_date"));
            var stored = (DateTime)dataObject.MasterRow!["hire_date"];
            Assert.Equal(new DateTime(2026, 5, 21, 14, 30, 0, DateTimeKind.Local), stored);
        }

        [Fact]
        [DisplayName("SetField 寫入 Guid 欄位字串後可讀回對應 Guid 值")]
        public void SetField_GuidColumn_ParsesGuidString()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            var newGuid = Guid.NewGuid();
            dataObject.SetField(SysFields.RowId, newGuid.ToString());

            Assert.Equal(newGuid, (Guid)dataObject.MasterRow![SysFields.RowId]);
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("SetField 寫入 base64 字串到 Binary 欄位後可讀回原始位元組")]
        public void SetField_BinaryColumn_ParsesBase64()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("raw_data", "Raw Data", FieldDbType.Binary);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();

            var bytes = new byte[] { 1, 2, 3 };
            dataObject.SetField("raw_data", Convert.ToBase64String(bytes));

            Assert.Equal(bytes, (byte[])dataObject.MasterRow!["raw_data"]);
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("SetField 空字串對非 nullable byte[] 欄位且無 DefaultValue 時回退到空陣列")]
        public async Task SetField_EmptyOnRawNonNullByteArrayColumn_FallsBackToEmpty()
        {
            var schema = BuildEmployeeSchema();

            // Server returns a DataSet with a raw byte[] column that has AllowDBNull=false
            // and no explicit DefaultValue (simulates ADO.NET server response).
            var serverDs = new DataSet(TestProgId);
            var serverTable = new DataTable(TestProgId);
            serverTable.Columns.Add(SysFields.RowId, typeof(Guid));
            var binaryCol = serverTable.Columns.Add("raw_data", typeof(byte[]));
            binaryCol.AllowDBNull = false;
            // DefaultValue is DBNull.Value by default for raw ADO.NET DataColumn
            serverTable.Rows.Add(Guid.NewGuid(), Array.Empty<byte>());
            serverDs.Tables.Add(serverTable);
            serverDs.AcceptChanges();

            var connector = new FakeFormApiConnector
            {
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = serverDs },
            };
            var dataObject = new FormDataObject(schema, connector);
            await dataObject.NewAsync();

            dataObject.SetField("raw_data", string.Empty);

            Assert.Equal(Array.Empty<byte>(), (byte[])dataObject.MasterRow!["raw_data"]);
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<GetNewDataResponse>? GetNewDataHandler { get; set; }

            public override Task<GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new GetNewDataResponse()))());
        }
    }
}
