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
    /// Verifies that <see cref="FormDataObject"/> derives the correct <see cref="DataSet"/>
    /// shape from <see cref="FormSchema"/>, round-trips values through
    /// <see cref="FormDataObject.GetField(string)"/> / <see cref="FormDataObject.SetField(string, string?)"/>, and
    /// drives the four async server methods through a supplied
    /// <see cref="FormApiConnector"/>. Mirrors the MAUI <c>FormDataObjectTests</c> for
    /// cross-family parity.
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
        [DisplayName("初始狀態 MasterRow 為 null,InitializeNewMaster 後存在一筆空列")]
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
        [DisplayName("SetField 寫入與現值相同的值時不標記 IsDirty(初始 render echo 防護)")]
        public void SetField_SameValue_DoesNotMarkDirty()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            // Avalonia controls echo the initial value back through TextChanged once
            // during construction; an identical write must not dirty the row.
            dataObject.SetField("emp_name", dataObject.GetField("emp_name"));

            Assert.False(dataObject.IsDirty);
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

            // FieldDbType.String defaults to string.Empty, so AllowDBNull is false and
            // the column reverts to the empty-string default rather than DBNull.
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

        // --- server round-trip via FormApiConnector ---

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
            // DataSet should be the schema-derived empty skeleton, not the loaded one.
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

        [Fact]
        [DisplayName("SetField 寫入新值時觸發一次 FieldValueChanged 並攜帶欄位名與綁定字串")]
        public void SetField_NewValue_RaisesFieldValueChangedOnce()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            var raised = new List<FieldValueChangedEventArgs>();
            dataObject.FieldValueChanged += (_, e) => raised.Add(e);

            dataObject.SetField("emp_name", "Alice");

            var args = Assert.Single(raised);
            Assert.Equal(TestProgId, args.TableName);
            Assert.Equal("emp_name", args.FieldName, ignoreCase: true);
            Assert.Equal("Alice", args.Value);
            Assert.Same(dataObject.MasterRow, args.Row);
        }

        [Fact]
        [DisplayName("明細列直接寫入 DataRow 也觸發 FieldValueChanged（事件橋接）並標記 dirty")]
        public void DetailRowDirectWrite_RaisesFieldValueChangedAndDirties()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;
            detail.Rows.Add("02-1234-5678");
            detail.AcceptChanges();
            // Reset the dirty flag accumulated while arranging the detail row.
            dataObject.InitializeNewMaster();

            var raised = new List<FieldValueChangedEventArgs>();
            dataObject.FieldValueChanged += (_, e) => raised.Add(e);

            detail.Rows[0]["phone"] = "0912-345-678";

            var args = Assert.Single(raised);
            Assert.Equal("EmployeePhone", args.TableName);
            Assert.Equal("phone", args.FieldName, ignoreCase: true);
            Assert.Equal("0912-345-678", args.Value);
            Assert.Same(detail.Rows[0], args.Row);
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("Detached 列（NewRow 未 Add）的寫入不觸發 FieldValueChanged")]
        public void DetachedRowWrite_DoesNotRaiseFieldValueChanged()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            var raisedCount = 0;
            dataObject.FieldValueChanged += (_, _) => raisedCount++;

            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;
            var row = detail.NewRow();
            row["phone"] = "02-1234-5678";

            Assert.Equal(0, raisedCount);
        }

        [Fact]
        [DisplayName("明細增列 / 刪列經橋接標記 dirty，AcceptChanges 不標")]
        public void DetailRowAddDelete_DirtyTracking()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            Assert.False(dataObject.IsDirty);

            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;
            detail.Rows.Add("02-1234-5678");
            Assert.True(dataObject.IsDirty);

            dataObject.InitializeNewMaster();
            Assert.False(dataObject.IsDirty);
            detail.AcceptChanges();
            Assert.False(dataObject.IsDirty);

            detail.Rows[0].Delete();
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("DataSet 置換後新表寫入仍觸發事件、舊表寫入不再觸發（重訂閱）")]
        public async Task ReplaceDataSet_MovesSubscriptionToNewTables()
        {
            var oldDataObjectSeedRowId = Guid.NewGuid();
            var refreshed = BuildServerDataSet(oldDataObjectSeedRowId, "Alice");
            var connector = new FakeFormApiConnector
            {
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = refreshed },
            };
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);
            dataObject.InitializeNewMaster();
            var oldMaster = dataObject.MasterTable;

            await dataObject.NewAsync();

            var raisedCount = 0;
            dataObject.FieldValueChanged += (_, _) => raisedCount++;

            // The retired table no longer feeds the bridge.
            oldMaster.Rows[0]["emp_name"] = "Stale";
            Assert.Equal(0, raisedCount);

            // The replacement table does.
            dataObject.SetField("emp_name", "Bob");
            Assert.Equal(1, raisedCount);
        }

        [Fact]
        [DisplayName("SetField 寫入相同值時不觸發 FieldValueChanged（echo 防護）")]
        public void SetField_SameValue_DoesNotRaiseFieldValueChanged()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            dataObject.SetField("emp_name", "Alice");

            var raisedCount = 0;
            dataObject.FieldValueChanged += (_, _) => raisedCount++;

            dataObject.SetField("emp_name", "Alice");

            Assert.Equal(0, raisedCount);
        }

        [Fact]
        [DisplayName("InitializeNewMaster 觸發 DataSetReplaced")]
        public void InitializeNewMaster_RaisesDataSetReplaced()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());

            var raisedCount = 0;
            dataObject.DataSetReplaced += (_, _) => raisedCount++;

            dataObject.InitializeNewMaster();

            Assert.Equal(1, raisedCount);
        }

        [Fact]
        [DisplayName("LoadAsync 置換 DataSet 後觸發 DataSetReplaced")]
        public async Task LoadAsync_Success_RaisesDataSetReplaced()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetDataHandler = _ => new GetDataResponse { DataSet = BuildServerDataSet(rowId, "Alice") },
            };
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);

            var raisedCount = 0;
            dataObject.DataSetReplaced += (_, _) => raisedCount++;

            await dataObject.LoadAsync(rowId);

            Assert.Equal(1, raisedCount);
        }

        [Fact]
        [DisplayName("SaveAsync 在 server 回傳 null DataSet 時不觸發 DataSetReplaced")]
        public async Task SaveAsync_NullRefreshedDataSet_DoesNotRaiseDataSetReplaced()
        {
            var connector = new FakeFormApiConnector
            {
                SaveHandler = _ => new SaveResponse(),
            };
            var dataObject = new FormDataObject(BuildEmployeeSchema(), connector);
            dataObject.InitializeNewMaster();

            var raisedCount = 0;
            dataObject.DataSetReplaced += (_, _) => raisedCount++;

            await dataObject.SaveAsync();

            Assert.Equal(0, raisedCount);
        }

        [Fact]
        [DisplayName("列編輯協定：BeginRowEdit 期間事件靜默，CommitRowEdit 只補發本次變更欄位並標 dirty")]
        public void RowEditProtocol_CommitPublishesSessionChangesOnly()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;
            detail.Rows.Add("02-1234-5678");
            detail.AcceptChanges();
            dataObject.InitializeNewMaster();
            var row = detail.Rows[0];

            var raised = new List<FieldValueChangedEventArgs>();
            dataObject.FieldValueChanged += (_, e) => raised.Add(e);

            dataObject.BeginRowEdit(row);
            dataObject.SetField(row, "phone", "0912-345-678");
            // ADO.NET suspends change events during an explicit edit session — pin it.
            Assert.Empty(raised);
            // Proposed value is readable through the row accessor during the session.
            Assert.Equal("0912-345-678", dataObject.GetField(row, "phone"));

            dataObject.CommitRowEdit(row);

            var args = Assert.Single(raised);
            Assert.Equal("EmployeePhone", args.TableName);
            Assert.Equal("phone", args.FieldName, ignoreCase: true);
            Assert.Equal("0912-345-678", args.Value);
            Assert.Same(row, args.Row);
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("列編輯協定：CancelRowEdit 完整還原且零事件、不弄髒")]
        public void RowEditProtocol_CancelRestoresSilently()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;
            detail.Rows.Add("02-1234-5678");
            detail.AcceptChanges();
            dataObject.InitializeNewMaster();
            var row = detail.Rows[0];

            var raisedCount = 0;
            dataObject.FieldValueChanged += (_, _) => raisedCount++;

            dataObject.BeginRowEdit(row);
            dataObject.SetField(row, "phone", "0912-345-678");
            dataObject.CancelRowEdit(row);

            Assert.Equal(0, raisedCount);
            Assert.Equal("02-1234-5678", dataObject.GetField(row, "phone"));
            Assert.False(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("列編輯協定：無變更的 Commit 不發事件、不弄髒")]
        public void RowEditProtocol_CommitWithoutChanges_StaysSilent()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;
            detail.Rows.Add("02-1234-5678");
            detail.AcceptChanges();
            dataObject.InitializeNewMaster();
            var row = detail.Rows[0];

            var raisedCount = 0;
            dataObject.FieldValueChanged += (_, _) => raisedCount++;

            dataObject.BeginRowEdit(row);
            dataObject.CommitRowEdit(row);

            Assert.Equal(0, raisedCount);
            Assert.False(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("列存取 API 拒絕不屬於本 DataSet 的列")]
        public void RowAccessors_ForeignRow_Throws()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            var foreign = new DataTable("Foreign");
            foreign.Columns.Add("x", typeof(string));
            foreign.Rows.Add("y");
            var foreignRow = foreign.Rows[0];

            Assert.Throws<ArgumentException>(() => dataObject.GetField(foreignRow, "x"));
            Assert.Throws<ArgumentException>(() => dataObject.SetField(foreignRow, "x", "z"));
            Assert.Throws<ArgumentException>(() => dataObject.BeginRowEdit(foreignRow));
        }

        [Fact]
        [DisplayName("新明細列由 FormSchema 補非空值（sys_rowid 新 Guid、master 連結、各型別預設、DBNull 型別補值）")]
        public void NewDetailRow_SeedsNonNullDefaultsFromSchema()
        {
            var schema = new FormSchema("Order", "Order");
            var master = schema.Tables!.Add("Order", "Order");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            var detail = schema.Tables.Add("OrderLine", "Lines");
            detail.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            detail.Fields.Add(SysFields.MasterRowId, "Master", FieldDbType.Guid);
            detail.Fields.Add("note", "Note", FieldDbType.String);
            detail.Fields.Add("qty", "Qty", FieldDbType.Integer);
            detail.Fields.Add("price", "Price", FieldDbType.Currency);
            detail.Fields.Add("order_date", "Date", FieldDbType.Date);
            detail.Fields.Add("product_rowid", "Product", FieldDbType.Guid);
            detail.Fields.Add("seq", "Seq", FieldDbType.Short);   // DBNull column default → proves seeding fills it

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var masterRowId = Guid.NewGuid();
            dataObject.MasterRow![SysFields.RowId] = masterRowId;

            var lineTable = dataObject.DataSet.Tables["OrderLine"]!;
            var line = lineTable.NewRow();
            lineTable.Rows.Add(line);

            Assert.NotEqual(Guid.Empty, (Guid)line[SysFields.RowId]);     // fresh primary key
            Assert.Equal(masterRowId, (Guid)line[SysFields.MasterRowId]); // linked to master
            Assert.Equal(string.Empty, line["note"]);
            Assert.Equal(0, line["qty"]);
            Assert.Equal(0m, line["price"]);
            Assert.Equal(DateTime.Today, line["order_date"]);
            Assert.Equal(Guid.Empty, (Guid)line["product_rowid"]);        // non-key Guid → empty
            Assert.NotEqual(DBNull.Value, line["seq"]);                   // seeded (no column default)
            Assert.Equal((short)0, line["seq"]);
        }

        [Fact]
        [DisplayName("新增明細經 BeginRowEdit/CommitRowEdit 後 SaveAsync 應送出該 Added 明細列")]
        public async Task AddDetail_EditFormFlow_ReachesSaveAsAddedRow()
        {
            var schema = new FormSchema("Order", "Order");
            var master = schema.Tables!.Add("Order", "Order");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            var detail = schema.Tables.Add("OrderLine", "Lines");
            detail.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            detail.Fields.Add(SysFields.MasterRowId, "Master", FieldDbType.Guid);
            detail.Fields.Add("product_rowid", "Product", FieldDbType.Guid);
            detail.Fields.Add("qty", "Qty", FieldDbType.Integer);

            DataSet? captured = null;
            var connector = new FakeFormApiConnector
            {
                SaveHandler = ds => { captured = ds.Copy(); return new SaveResponse { DataSet = ds }; },
            };
            var dataObject = new FormDataObject(schema, connector);
            dataObject.InitializeNewMaster();
            var masterRowId = (Guid)dataObject.MasterRow![SysFields.RowId];

            var lineTable = dataObject.DataSet.Tables["OrderLine"]!;
            var line = lineTable.NewRow();   // TableNewRow seeds sys_rowid + master link + defaults
            lineTable.Rows.Add(line);

            // Mirror the EditForm dialog: begin a buffered edit, fill fields, commit.
            dataObject.BeginRowEdit(line);
            line["product_rowid"] = Guid.NewGuid();
            line["qty"] = 5;
            dataObject.CommitRowEdit(line);

            Assert.Equal(DataRowState.Added, line.RowState);

            await dataObject.SaveAsync();

            Assert.NotNull(captured);
            var capturedDetail = captured!.Tables["OrderLine"]!;
            Assert.Single(capturedDetail.Rows);
            Assert.Equal(DataRowState.Added, capturedDetail.Rows[0].RowState);
            Assert.Equal(masterRowId, (Guid)capturedDetail.Rows[0][SysFields.MasterRowId]);
            Assert.Equal(5, capturedDetail.Rows[0]["qty"]);
        }

        [Fact]
        [DisplayName("明細新增列觸發 RowAdded（帶 TableName + Row）")]
        public void RowAdded_OnDetailRowAdd_Fires()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;

            RowChangedEventArgs? captured = null;
            dataObject.RowAdded += (_, e) => captured = e;

            var row = detail.NewRow();
            detail.Rows.Add(row);

            Assert.NotNull(captured);
            Assert.Equal("EmployeePhone", captured!.TableName);
            Assert.Same(row, captured.Row);
        }

        [Fact]
        [DisplayName("明細刪除列觸發 RowDeleted（帶 TableName + Row）")]
        public void RowDeleted_OnDetailRowDelete_Fires()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();
            var detail = dataObject.DataSet.Tables["EmployeePhone"]!;
            var row = detail.NewRow();
            detail.Rows.Add(row);
            detail.AcceptChanges();

            RowChangedEventArgs? captured = null;
            dataObject.RowDeleted += (_, e) => captured = e;

            row.Delete();

            Assert.NotNull(captured);
            Assert.Equal("EmployeePhone", captured!.TableName);
        }

        [Fact]
        [DisplayName("IsDirtyChanged 只在值翻轉時觸發,重複弄髒不重發")]
        public void IsDirtyChanged_FiresOnlyOnTransition()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            var transitions = new List<bool>();
            dataObject.IsDirtyChanged += (_, _) => transitions.Add(dataObject.IsDirty);

            dataObject.SetField("emp_name", "Alice");
            dataObject.SetField("emp_name", "Bob");

            // One transition so far: clean -> dirty.
            Assert.Equal([true], transitions);

            // Reset to clean -> a second transition: dirty -> clean.
            dataObject.InitializeNewMaster();
            Assert.Equal([true, false], transitions);
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
