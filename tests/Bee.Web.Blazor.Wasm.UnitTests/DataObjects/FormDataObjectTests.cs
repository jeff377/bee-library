using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Web.Blazor.Wasm.DataObjects;

namespace Bee.Web.Blazor.Wasm.UnitTests.DataObjects
{
    /// <summary>
    /// Verifies that <see cref="FormDataObject"/> derives the correct <see cref="DataSet"/>
    /// shape from <see cref="FormSchema"/> and round-trips values through
    /// <see cref="FormDataObject.GetField"/> / <see cref="FormDataObject.SetField"/>.
    /// </summary>
    public class FormDataObjectTests
    {
        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_id", "Employee ID", FieldDbType.String);
            master.Fields.Add("emp_name", "Name", FieldDbType.String);
            master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            master.Fields.Add("is_active", "Active", FieldDbType.Boolean);
            master.Fields.Add("salary", "Salary", FieldDbType.Decimal);
            master.Fields.Add("manager_rowid", "Manager", FieldDbType.Long);

            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);

            return schema;
        }

        [Fact]
        [DisplayName("由 FormSchema 推導出對應的 DataSet 與欄位")]
        public void Constructor_FromSchema_BuildsExpectedDataSetShape()
        {
            var schema = BuildEmployeeSchema();
            var dataObject = new FormDataObject(schema);

            Assert.Equal("Employee", dataObject.DataSet.DataSetName);
            Assert.Equal(2, dataObject.DataSet.Tables.Count);

            var master = dataObject.MasterTable;
            Assert.Equal("Employee", master.TableName);
            Assert.Equal(6, master.Columns.Count);
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
        [DisplayName("SetField 寫入 Boolean 欄位後 GetField 回傳 True/False 字串")]
        public void SetField_Boolean_RoundTripsThroughGetField()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            dataObject.InitializeNewMaster();

            dataObject.SetField("is_active", "True");
            Assert.Equal("True", dataObject.GetField("is_active"));
            Assert.Equal(true, dataObject.MasterRow!["is_active"]);

            dataObject.SetField("is_active", "False");
            Assert.Equal("False", dataObject.GetField("is_active"));
            Assert.Equal(false, dataObject.MasterRow["is_active"]);
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

        [Fact]
        [DisplayName("LoadAsync 在 Phase 1a 階段拋出 NotImplementedException")]
        public async Task LoadAsync_Phase1a_Throws()
        {
            await Assert.ThrowsAsync<NotImplementedException>(() => FormDataObject.LoadAsync(new object()));
        }

        [Fact]
        [DisplayName("SaveAsync 在 Phase 1a 階段拋出 NotImplementedException")]
        public async Task SaveAsync_Phase1a_Throws()
        {
            await Assert.ThrowsAsync<NotImplementedException>(() => FormDataObject.SaveAsync());
        }

        [Fact]
        [DisplayName("DeleteAsync 在 Phase 1a 階段拋出 NotImplementedException")]
        public async Task DeleteAsync_Phase1a_Throws()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            await Assert.ThrowsAsync<NotImplementedException>(() => dataObject.DeleteAsync());
        }

        [Fact]
        [DisplayName("NewAsync 在 Phase 1a 階段拋出 NotImplementedException")]
        public async Task NewAsync_Phase1a_Throws()
        {
            var dataObject = new FormDataObject(BuildEmployeeSchema());
            await Assert.ThrowsAsync<NotImplementedException>(() => dataObject.NewAsync());
        }
    }
}
