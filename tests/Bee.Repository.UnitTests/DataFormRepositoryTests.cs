using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using Bee.Base.Data;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Form;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="DataFormRepository"/> 建構子驗證、<see cref="DataFormRepository.GetNewData"/>
    /// 及私有靜態輔助方法（<c>ConvertDefaultValue</c>、<c>TryCoerceToGuid</c>）的純邏輯測試，
    /// 不需資料庫連線。
    /// </summary>
    public class DataFormRepositoryTests
    {
        #region Stubs

        private sealed class StubDefineAccess : IDefineAccess
        {
            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            public ProgramSettings GetProgramSettings() => throw new NotImplementedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
        }

        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private sealed class StubConnectionManager : IDbConnectionManager
        {
            public DbConnectionInfo GetConnectionInfo(string databaseId) => throw new NotImplementedException();
            public DbConnection CreateConnection(string databaseId) => throw new NotImplementedException();
            public bool Remove(string databaseId) => false;
            public void Clear() { }
            public bool Contains(string databaseId) => false;
            public int Count => 0;
        }

        // CA1861: 所有 typeof() 陣列抽成 static readonly
        private static readonly Type[] s_convertDefaultValueParams = [typeof(string), typeof(Type)];
        private static readonly Type[] s_tryCoerceToGuidParams = [typeof(object)];

        private static DataFormRepository CreateRepository(FormSchema? schema = null)
        {
            schema ??= BuildSchema();
            return new DataFormRepository(
                "Employee",
                schema,
                new StubDefineAccess(),
                new StubDbAccessFactory(),
                new StubConnectionManager(),
                "testdb");
        }

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("emp_name", "Name", FieldDbType.String);
            return schema;
        }

        private static MethodInfo GetPrivateStaticMethod(string name, Type[] paramTypes)
        {
            var method = typeof(DataFormRepository).GetMethod(
                name, BindingFlags.NonPublic | BindingFlags.Static, null, paramTypes, null);
            Assert.NotNull(method);
            return method!;
        }

        #endregion

        #region 建構子驗證

        [Fact]
        [DisplayName("DataFormRepository 建構子傳入 null progId 應拋 ArgumentNullException")]
        public void Constructor_NullProgId_ThrowsArgumentNullException()
        {
            var schema = BuildSchema();
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository(null!, schema, new StubDefineAccess(),
                    new StubDbAccessFactory(), new StubConnectionManager(), "testdb"));
        }

        [Fact]
        [DisplayName("DataFormRepository 建構子傳入 null schema 應拋 ArgumentNullException")]
        public void Constructor_NullSchema_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository("Employee", null!, new StubDefineAccess(),
                    new StubDbAccessFactory(), new StubConnectionManager(), "testdb"));
        }

        [Fact]
        [DisplayName("DataFormRepository 建構子傳入 null defineAccess 應拋 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository("Employee", BuildSchema(), null!,
                    new StubDbAccessFactory(), new StubConnectionManager(), "testdb"));
        }

        [Fact]
        [DisplayName("DataFormRepository 建構子傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository("Employee", BuildSchema(), new StubDefineAccess(),
                    null!, new StubConnectionManager(), "testdb"));
        }

        [Fact]
        [DisplayName("DataFormRepository 建構子傳入 null connectionManager 應拋 ArgumentNullException")]
        public void Constructor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository("Employee", BuildSchema(), new StubDefineAccess(),
                    new StubDbAccessFactory(), null!, "testdb"));
        }

        [Fact]
        [DisplayName("DataFormRepository 建構子傳入 null databaseId 應拋 ArgumentNullException")]
        public void Constructor_NullDatabaseId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DataFormRepository("Employee", BuildSchema(), new StubDefineAccess(),
                    new StubDbAccessFactory(), new StubConnectionManager(), null!));
        }

        [Fact]
        [DisplayName("DataFormRepository 建構子正確設定 ProgId 屬性")]
        public void Constructor_ValidArgs_SetsProgId()
        {
            var repo = CreateRepository();
            Assert.Equal("Employee", repo.ProgId);
        }

        #endregion

        #region GetNewData

        [Fact]
        [DisplayName("GetNewData Schema 含 MasterTable 應回傳含一列 master 資料的 DataSet")]
        public void GetNewData_SchemaWithMasterTable_ReturnsDataSetWithOneRow()
        {
            var repo = CreateRepository();
            var dataSet = repo.GetNewData();

            Assert.NotNull(dataSet);
            Assert.Equal("Employee", dataSet.DataSetName);
            Assert.True(dataSet.Tables.Contains("Employee"));
            Assert.Equal(1, dataSet.Tables["Employee"]!.Rows.Count);
        }

        [Fact]
        [DisplayName("GetNewData master 列的 sys_rowid 應為非空 Guid")]
        public void GetNewData_MasterRow_HasNonEmptyRowId()
        {
            var repo = CreateRepository();
            var dataSet = repo.GetNewData();
            var masterRow = dataSet.Tables["Employee"]!.Rows[0];
            var rowId = (Guid)masterRow[SysFields.RowId];
            Assert.NotEqual(Guid.Empty, rowId);
        }

        [Fact]
        [DisplayName("GetNewData Schema 無 MasterTable 應拋 InvalidOperationException")]
        public void GetNewData_SchemaWithoutMasterTable_ThrowsInvalidOperationException()
        {
            var schema = new FormSchema("NoMaster", "NoMaster");
            var repo = CreateRepository(schema);
            Assert.Throws<InvalidOperationException>(() => repo.GetNewData());
        }

        #endregion

        #region ConvertDefaultValue（私有靜態方法）

        [Theory]
        [InlineData("hello world")]
        [InlineData("")]
        [DisplayName("ConvertDefaultValue string 型別應直接回傳原始字串")]
        public void ConvertDefaultValue_StringType_ReturnsRawString(string rawValue)
        {
            var method = GetPrivateStaticMethod("ConvertDefaultValue", s_convertDefaultValueParams);
            // rawValue 為變數，避免 CA1861（new object[] { variable, typeof(...) } 不觸發）
            var result = method.Invoke(null, new object[] { rawValue, typeof(string) });
            Assert.Equal(rawValue, result);
        }

        [Fact]
        [DisplayName("ConvertDefaultValue Guid 型別傳入有效 Guid 字串應解析回傳")]
        public void ConvertDefaultValue_GuidType_ValidString_ReturnsParsedGuid()
        {
            var method = GetPrivateStaticMethod("ConvertDefaultValue", s_convertDefaultValueParams);
            var expected = Guid.NewGuid();
            // expected.ToString() 為變數，避免 CA1861
            var result = method.Invoke(null, new object[] { expected.ToString(), typeof(Guid) });
            Assert.Equal(expected, (Guid)result!);
        }

        [Theory]
        [InlineData("not-a-guid")]
        [InlineData("12345")]
        [DisplayName("ConvertDefaultValue Guid 型別傳入無效字串應回傳 Guid.Empty")]
        public void ConvertDefaultValue_GuidType_InvalidString_ReturnsGuidEmpty(string invalidGuid)
        {
            var method = GetPrivateStaticMethod("ConvertDefaultValue", s_convertDefaultValueParams);
            var result = method.Invoke(null, new object[] { invalidGuid, typeof(Guid) });
            Assert.Equal(Guid.Empty, (Guid)result!);
        }

        [Theory]
        [InlineData("42", 42)]
        [InlineData("0", 0)]
        [DisplayName("ConvertDefaultValue int 型別傳入有效數字字串應轉換回傳")]
        public void ConvertDefaultValue_IntType_ValidString_ReturnsInt(string raw, int expected)
        {
            var method = GetPrivateStaticMethod("ConvertDefaultValue", s_convertDefaultValueParams);
            var result = method.Invoke(null, new object[] { raw, typeof(int) });
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("not-a-number")]
        [InlineData("abc")]
        [DisplayName("ConvertDefaultValue int 型別傳入無效字串應回傳 DBNull.Value")]
        public void ConvertDefaultValue_IntType_InvalidString_ReturnsDBNull(string invalidInt)
        {
            var method = GetPrivateStaticMethod("ConvertDefaultValue", s_convertDefaultValueParams);
            var result = method.Invoke(null, new object[] { invalidInt, typeof(int) });
            Assert.Same(DBNull.Value, result);
        }

        #endregion

        #region TryCoerceToGuid（私有靜態方法）

        [Fact]
        [DisplayName("TryCoerceToGuid 傳入 Guid 應直接回傳相同 Guid")]
        public void TryCoerceToGuid_GuidInput_ReturnsSameGuid()
        {
            var method = GetPrivateStaticMethod("TryCoerceToGuid", s_tryCoerceToGuidParams);
            var expected = Guid.NewGuid();
            // expected 為變數，不觸發 CA1861
            var result = method.Invoke(null, new object?[] { expected });
            Assert.Equal(expected, (Guid)result!);
        }

        [Fact]
        [DisplayName("TryCoerceToGuid 傳入有效 Guid 字串應解析並回傳 Guid")]
        public void TryCoerceToGuid_ValidGuidString_ReturnsParsedGuid()
        {
            var method = GetPrivateStaticMethod("TryCoerceToGuid", s_tryCoerceToGuidParams);
            var expected = Guid.NewGuid();
            // expected.ToString() 為變數，不觸發 CA1861
            var result = method.Invoke(null, new object?[] { expected.ToString() });
            Assert.Equal(expected, (Guid)result!);
        }

        [Theory]
        [InlineData("not-a-guid")]
        [InlineData("12345")]
        [DisplayName("TryCoerceToGuid 傳入無效字串應回傳 null")]
        public void TryCoerceToGuid_InvalidString_ReturnsNull(string invalidGuid)
        {
            var method = GetPrivateStaticMethod("TryCoerceToGuid", s_tryCoerceToGuidParams);
            var result = method.Invoke(null, new object?[] { invalidGuid });
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("TryCoerceToGuid 傳入 null 應回傳 null")]
        public void TryCoerceToGuid_NullInput_ReturnsNull()
        {
            var method = GetPrivateStaticMethod("TryCoerceToGuid", s_tryCoerceToGuidParams);
            // 使用本地變數 nullValue 避免 new object?[] { null } 觸發 CA1861
            object? nullValue = null;
            var result = method.Invoke(null, new object?[] { nullValue });
            Assert.Null(result);
        }

        #endregion
    }
}
