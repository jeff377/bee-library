using System.ComponentModel;
using System.Data;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.DataObjects
{
    /// <summary>
    /// Verifies that <see cref="FormDataObject"/> derives the correct <see cref="DataSet"/>
    /// shape from <see cref="FormSchema"/>, round-trips values through
    /// <see cref="FormDataObject.GetField"/> / <see cref="FormDataObject.SetField"/>, and
    /// drives the four async server methods through a supplied
    /// <see cref="FormApiConnector"/>.
    /// </summary>
    public class FormDataObjectTests
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
            master.Fields.Add("is_active", "Active", FieldDbType.Boolean);
            master.Fields.Add("salary", "Salary", FieldDbType.Decimal);
            master.Fields.Add("manager_rowid", "Manager", FieldDbType.Long);

            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);

            return schema;
        }

        private static DataSet BuildServerDataSet(Guid rowId, string empName)
        {
            var dataSet = new DataSet(TestProgId);
            var master = new DataTable(TestProgId);
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add("emp_name", typeof(string));
            master.Rows.Add(rowId, empName);
            dataSet.Tables.Add(master);
            dataSet.AcceptChanges();
            return dataSet;
        }

        [Fact]
        [DisplayName("由 FormSchema 推導出對應的 DataSet 與欄位")]
        public void Constructor_FromSchema_BuildsExpectedDataSetShape()
        {
            var schema = BuildEmployeeSchema();
            var dataObject = new FormDataObject(schema);

            Assert.Equal(TestProgId, dataObject.DataSet.DataSetName);
            Assert.Equal(2, dataObject.DataSet.Tables.Count);

            var master = dataObject.MasterTable;
            Assert.Equal(TestProgId, master.TableName);
            Assert.Equal(7, master.Columns.Count);
            Assert.True(master.Columns.Contains("emp_id"));
            Assert.True(master.Columns.Contains("hire_date"));
            Assert.Equal(typeof(DateTime), master.Columns["hire_date"]!.DataType);
            Assert.Equal(typeof(bool), master.Columns["is_active"]!.DataType);
            Assert.Equal(typeof(decimal), master.Columns["salary"]!.DataType);

            var details = dataObject.DetailTables.ToList();
            Assert.Single(details);
            Assert.Equal("EmployeePhone", details[0].TableName);
        }

        [Fact]
        [DisplayName("初始狀態 MasterRow 為 null，InitializeNewMaster 後存在一筆空列")]
        public void InitializeNewMaster_AddsSingleEmptyRow()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());

            Assert.Null(dataObject.MasterRow);

            dataObject.InitializeNewMaster();

            Assert.NotNull(dataObject.MasterRow);
            Assert.Equal(1, dataObject.MasterTable.Rows.Count);
            Assert.False(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("GetField 在無 MasterRow 時回傳空字串")]
        public void GetField_NoMasterRow_ReturnsEmpty()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());

            Assert.Equal(string.Empty, dataObject.GetField("emp_id"));
        }

        [Fact]
        [DisplayName("SetField 在無 MasterRow 時為 no-op")]
        public void SetField_NoMasterRow_IsNoOp()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());

            dataObject.SetField("emp_id", "E001");

            Assert.Null(dataObject.MasterRow);
            Assert.False(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("SetField 寫入字串欄位後 GetField 可讀回相同值")]
        public void SetField_String_RoundTripsThroughGetField()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            dataObject.SetField("emp_name", "Alice");

            Assert.Equal("Alice", dataObject.GetField("emp_name"));
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("SetField 寫入 Boolean 欄位後 GetField 回傳 True/False 字串")]
        public void SetField_Boolean_RoundTripsThroughGetField()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            dataObject.SetField("is_active", "True");
            Assert.Equal("True", dataObject.GetField("is_active"));
            Assert.True((bool)dataObject.MasterRow!["is_active"]);

            dataObject.SetField("is_active", "False");
            Assert.Equal("False", dataObject.GetField("is_active"));
            Assert.False((bool)dataObject.MasterRow["is_active"]);
        }

        [Fact]
        [DisplayName("SetField 寫入 Date 欄位後 GetField 回傳 ISO yyyy-MM-dd 格式")]
        public void SetField_Date_RoundTripsAsIsoDate()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            dataObject.SetField("hire_date", "2026-05-21");

            Assert.Equal("2026-05-21", dataObject.GetField("hire_date"));
            var stored = (DateTime)dataObject.MasterRow!["hire_date"];
            Assert.Equal(new DateTime(2026, 5, 21), stored);
        }

        [Fact]
        [DisplayName("SetField 寫入 Decimal 欄位後 GetField 回傳 Invariant 格式")]
        public void SetField_Decimal_UsesInvariantFormatting()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            dataObject.SetField("salary", "1234.56");

            Assert.Equal("1234.56", dataObject.GetField("salary"));
            Assert.Equal(1234.56m, dataObject.MasterRow!["salary"]);
        }

        [Fact]
        [DisplayName("SetField 寫入空字串對允許 DBNull 的欄位會設為 DBNull")]
        public void SetField_EmptyString_OnNullableColumn_SetsDbNull()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            dataObject.SetField("manager_rowid", "42");
            Assert.Equal(42L, dataObject.MasterRow!["manager_rowid"]);

            dataObject.SetField("manager_rowid", string.Empty);

            Assert.Equal(DBNull.Value, dataObject.MasterRow["manager_rowid"]);
            Assert.Equal(string.Empty, dataObject.GetField("manager_rowid"));
        }

        [Fact]
        [DisplayName("SetField 寫入空字串對 NOT NULL 欄位會回退到欄位預設值")]
        public void SetField_EmptyString_OnNotNullColumn_FallsBackToDefault()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            dataObject.SetField("emp_name", "Bob");

            dataObject.SetField("emp_name", string.Empty);

            Assert.Equal(string.Empty, dataObject.MasterRow!["emp_name"]);
            Assert.Equal(string.Empty, dataObject.GetField("emp_name"));
        }

        [Fact]
        [DisplayName("GetField/SetField 在欄位不存在時不丟例外")]
        public void GetField_AndSetField_UnknownColumn_AreTolerated()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            Assert.Equal(string.Empty, dataObject.GetField("not_a_column"));

            var exception = Record.Exception(() => dataObject.SetField("not_a_column", "x"));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("GetFormField 可回傳 master table 上的 FormField 元資料")]
        public void GetFormField_ReturnsMasterFieldMetadata()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());

            var field = dataObject.GetFormField("emp_id");
            Assert.NotNull(field);
            Assert.Equal("Employee ID", field!.Caption);

            Assert.Null(dataObject.GetFormField("not_a_field"));
        }

        [Fact]
        [DisplayName("建構子在 schema 為 null 時拋出 ArgumentNullException")]
        public void Constructor_NullSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FormDataObject(null!));
        }

        [Fact]
        [DisplayName("建構子在 ProgId 為空字串時拋出 ArgumentException")]
        public void Constructor_EmptyProgId_Throws()
        {
            var schema = new FormSchema();
            Assert.Throws<ArgumentException>(() => new FormDataObject(schema));
        }

        [Fact]
        [DisplayName("LoadAsync 在無 connector 時拋出 InvalidOperationException")]
        public async Task LoadAsync_NoConnector_Throws()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            await Assert.ThrowsAsync<InvalidOperationException>(() => dataObject.LoadAsync(Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("LoadAsync 成功時以伺服器 DataSet 取代本地並重置 IsDirty")]
        public async Task LoadAsync_Success_ReplacesDataSetAndResetsDirty()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var serverDataSet = BuildServerDataSet(rowId, "Alice");
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = id => new GetDataResponse { DataSet = id == rowId ? serverDataSet : null },
            };
            var dataObject = new FormDataObject(schema, connector);
            dataObject.InitializeNewMaster();
            dataObject.SetField("emp_name", "stale");
            Assert.True(dataObject.IsDirty);

            await dataObject.LoadAsync(rowId);

            Assert.Same(serverDataSet, dataObject.DataSet);
            Assert.NotNull(dataObject.MasterRow);
            Assert.Equal(rowId, dataObject.MasterRow!["sys_rowid"]);
            Assert.Equal("Alice", dataObject.GetField("emp_name"));
            Assert.False(dataObject.IsDirty);
            Assert.False(dataObject.IsLoading);
        }

        [Fact]
        [DisplayName("LoadAsync 在 server 回傳 null DataSet 時拋出 InvalidOperationException")]
        public async Task LoadAsync_NotFound_Throws()
        {
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = _ => new GetDataResponse { DataSet = null },
            };
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);

            await Assert.ThrowsAsync<InvalidOperationException>(() => dataObject.LoadAsync(Guid.NewGuid()));
            Assert.False(dataObject.IsLoading);
        }

        [Fact]
        [DisplayName("NewAsync 成功時以伺服器骨架取代本地並重置 IsDirty")]
        public async Task NewAsync_Success_ReplacesDataSetAndResetsDirty()
        {
            var rowId = Guid.NewGuid();
            var serverDataSet = BuildServerDataSet(rowId, string.Empty);
            var connector = new FakeFormApiConnector
            {
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = serverDataSet },
            };
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);

            await dataObject.NewAsync();

            Assert.Same(serverDataSet, dataObject.DataSet);
            Assert.Equal(rowId, dataObject.MasterRow!["sys_rowid"]);
            Assert.False(dataObject.IsDirty);
            Assert.False(dataObject.IsLoading);
        }

        [Fact]
        [DisplayName("NewAsync 在 server 回傳 null DataSet 時拋出 InvalidOperationException")]
        public async Task NewAsync_NullDataSet_Throws()
        {
            var connector = new FakeFormApiConnector
            {
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = null },
            };
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);

            await Assert.ThrowsAsync<InvalidOperationException>(() => dataObject.NewAsync());
        }

        [Fact]
        [DisplayName("NewAsync 在無 connector 時拋出 InvalidOperationException")]
        public async Task NewAsync_NoConnector_Throws()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            await Assert.ThrowsAsync<InvalidOperationException>(() => dataObject.NewAsync());
        }

        [Fact]
        [DisplayName("SaveAsync 成功時以伺服器 refreshed DataSet 取代本地並重置 IsDirty")]
        public async Task SaveAsync_Success_ReplacesDataSetAndResetsDirty()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var refreshed = BuildServerDataSet(rowId, "Persisted");
            DataSet? capturedRequest = null;
            var connector = new FakeFormApiConnector
            {
                SaveHandler = ds =>
                {
                    capturedRequest = ds;
                    return new SaveResponse { DataSet = refreshed };
                },
            };
            var dataObject = new FormDataObject(schema, connector);
            dataObject.InitializeNewMaster();
            dataObject.SetField("emp_name", "Pending");
            Assert.True(dataObject.IsDirty);
            var requestedDataSet = dataObject.DataSet;

            await dataObject.SaveAsync();

            Assert.Same(requestedDataSet, capturedRequest);
            Assert.Same(refreshed, dataObject.DataSet);
            Assert.Equal("Persisted", dataObject.GetField("emp_name"));
            Assert.False(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("SaveAsync 在 server 回傳 null DataSet 時保留本地內容並重置 IsDirty")]
        public async Task SaveAsync_NullRefreshedDataSet_KeepsLocalAndResetsDirty()
        {
            var connector = new FakeFormApiConnector
            {
                SaveHandler = _ => new SaveResponse { DataSet = null },
            };
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);
            dataObject.InitializeNewMaster();
            dataObject.SetField("emp_name", "Pending");
            var beforeDataSet = dataObject.DataSet;
            Assert.True(dataObject.IsDirty);

            await dataObject.SaveAsync();

            Assert.Same(beforeDataSet, dataObject.DataSet);
            Assert.False(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("SaveAsync 在無 connector 時拋出 InvalidOperationException")]
        public async Task SaveAsync_NoConnector_Throws()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            await Assert.ThrowsAsync<InvalidOperationException>(() => dataObject.SaveAsync());
        }

        [Fact]
        [DisplayName("DeleteAsync 成功時呼叫 connector 並重設為空白 DataSet")]
        public async Task DeleteAsync_Success_ResetsToEmptyDataSet()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var loaded = BuildServerDataSet(rowId, "Alice");
            Guid? deletedRowId = null;
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = _ => new GetDataResponse { DataSet = loaded },
                DeleteHandler = id =>
                {
                    deletedRowId = id;
                    return new DeleteResponse { RowsAffected = 1 };
                },
            };
            var dataObject = new FormDataObject(schema, connector);
            await dataObject.LoadAsync(rowId);

            await dataObject.DeleteAsync();

            Assert.Equal(rowId, deletedRowId);
            Assert.Null(dataObject.MasterRow);
            Assert.False(dataObject.IsDirty);
            Assert.NotSame(loaded, dataObject.DataSet);
            Assert.Equal(TestProgId, dataObject.DataSet.DataSetName);
            Assert.Equal(2, dataObject.DataSet.Tables.Count);
        }

        [Fact]
        [DisplayName("DeleteAsync 在無 MasterRow 時拋出 InvalidOperationException")]
        public async Task DeleteAsync_NoMasterRow_Throws()
        {
            var connector = new FakeFormApiConnector();
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);

            await Assert.ThrowsAsync<InvalidOperationException>(() => dataObject.DeleteAsync());
        }

        [Fact]
        [DisplayName("DeleteAsync 在無 connector 時拋出 InvalidOperationException")]
        public async Task DeleteAsync_NoConnector_Throws()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            await Assert.ThrowsAsync<InvalidOperationException>(() => dataObject.DeleteAsync());
        }

        /// <summary>
        /// Test double that bypasses the real JSON-RPC pipeline by overriding every
        /// virtual CRUD method on <see cref="FormApiConnector"/>. The base constructor
        /// still installs a <see cref="Bee.Api.Client.Providers.LocalApiProvider"/>, but
        /// because all four methods short-circuit before reaching it the provider is
        /// never invoked.
        /// </summary>
        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<Guid, GetDataResponse>? GetDataHandler { get; set; }
            public Func<GetNewDataResponse>? GetNewDataHandler { get; set; }
            public Func<DataSet, SaveResponse>? SaveHandler { get; set; }
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetDataResponse> GetDataAsync(Guid rowId)
                => Task.FromResult((GetDataHandler ?? (_ => new GetDataResponse()))(rowId));

            public override Task<GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new GetNewDataResponse()))());

            public override Task<SaveResponse> SaveAsync(DataSet dataSet)
                => Task.FromResult((SaveHandler ?? (_ => new SaveResponse()))(dataSet));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }
    }
}
