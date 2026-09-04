using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Api.Client.UnitTests.FormData
{
    /// <summary>
    /// <see cref="FormValueBinding"/> 的值轉換規則。
    /// </summary>
    /// <remarks>
    /// 這些規則原本各自私有地存在於兩個 UI head，doc 自承「沒有任何機制強制兩邊一致」，
    /// 而兩邊確實漂了。下沉為單一實作之後，這裡是它唯一的驗證點。
    /// </remarks>
    public class FormValueBindingTests
    {
        private static DataColumn Column(Type type, bool allowDBNull, object? defaultValue = null)
        {
            var table = new DataTable("t");
            var column = new DataColumn("c", type) { AllowDBNull = allowDBNull };
            table.Columns.Add(column);
            if (defaultValue is not null) { column.DefaultValue = defaultValue; }
            return column;
        }

        [Fact]
        [DisplayName("空字串寫入可為空的欄位應得 DBNull")]
        public void ToColumnValue_EmptyIntoNullableColumn_ReturnsDBNull()
        {
            Assert.Equal(DBNull.Value, FormValueBinding.ToColumnValue(string.Empty, Column(typeof(string), allowDBNull: true)));
        }

        [Fact]
        [DisplayName("空字串寫入 NOT NULL 欄位，應採用該欄位已設好的預設值")]
        public void ToColumnValue_EmptyIntoNonNullableColumn_UsesSeededDefault()
        {
            var column = Column(typeof(string), allowDBNull: false, defaultValue: "seeded");

            Assert.Equal("seeded", FormValueBinding.ToColumnValue(string.Empty, column));
        }

        [Theory]
        [InlineData(typeof(string), "")]
        [InlineData(typeof(int), 0)]
        [DisplayName("回歸：NOT NULL 欄位的 DefaultValue 仍是 DBNull 時，不得寫回 DBNull")]
        public void ToColumnValue_EmptyIntoNonNullableColumnWithUnseededDefault_ReturnsNonNull(Type type, object expected)
        {
            // Blazor head 的私有副本在這個情境回 DBNull，寫進 NOT NULL 欄位後 EndEdit 會擲
            // NoNullAllowedException；Avalonia head 早已修好而另一邊沒跟。伺服器回應常帶的是
            // 原始 ADO.NET column（DefaultValue 還是 DBNull），所以這不是邊角情境。
            var column = Column(type, allowDBNull: false);
            Assert.Equal(DBNull.Value, column.DefaultValue); // 前提：這個欄位確實沒被 seed 過

            var actual = FormValueBinding.ToColumnValue(string.Empty, column);

            Assert.NotEqual(DBNull.Value, actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("string", typeof(string), "string")]
        [DisplayName("字串欄位原樣寫入")]
        public void ToColumnValue_String_PassesThrough(string value, Type type, string expected)
        {
            Assert.Equal(expected, FormValueBinding.ToColumnValue(value, Column(type, allowDBNull: true)));
        }

        [Fact]
        [DisplayName("Guid / byte[] / DateTime 各走自己的剖析路徑（Convert.ChangeType 對前兩者無效）")]
        public void ToColumnValue_NonConvertibleTypes_UseDedicatedParsing()
        {
            var guid = Guid.NewGuid();
            Assert.Equal(guid, FormValueBinding.ToColumnValue(guid.ToString(), Column(typeof(Guid), allowDBNull: true)));

            byte[] bytes = [1, 2, 3];
            Assert.Equal(bytes, FormValueBinding.ToColumnValue(Convert.ToBase64String(bytes), Column(typeof(byte[]), allowDBNull: true)));

            Assert.Equal(
                new DateTime(2026, 9, 4, 13, 5, 0, DateTimeKind.Local),
                FormValueBinding.ToColumnValue("2026-09-04T13:05:00", Column(typeof(DateTime), allowDBNull: true)));
        }

        [Fact]
        [DisplayName("純日期以 yyyy-MM-dd 呈現，帶時間才加 T 時分秒")]
        public void ToBindingString_DateTime_UsesIso8601ByPrecision()
        {
            Assert.Equal("2026-09-04", FormValueBinding.ToBindingString(new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Local)));
            Assert.Equal("2026-09-04T13:05:00", FormValueBinding.ToBindingString(new DateTime(2026, 9, 4, 13, 5, 0, DateTimeKind.Local)));
        }

        [Fact]
        [DisplayName("數值以 InvariantCulture 呈現，不隨執行緒文化改變")]
        public void ToBindingString_Numeric_IsCultureInvariant()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                // de-DE 的小數點是逗號；沒有 InvariantCulture 這條會回 "1,5"。
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal("1.5", FormValueBinding.ToBindingString(1.5m));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Fact]
        [DisplayName("null 與 DBNull 都呈現為空字串")]
        public void ToBindingString_NullAndDBNull_ReturnEmpty()
        {
            Assert.Equal(string.Empty, FormValueBinding.ToBindingString(null));
            Assert.Equal(string.Empty, FormValueBinding.ToBindingString(DBNull.Value));
        }

        [Fact]
        [DisplayName("BuildEmptyDataSet 依 schema 建出對應的空資料表")]
        public void BuildEmptyDataSet_CreatesOneTablePerSchemaTable()
        {
            var schema = new FormSchema { ProgId = "test_form" };
            var table = new FormTable { TableName = "master" };
            table.Fields!.Add(new FormField { FieldName = "name", DbType = FieldDbType.String });
            schema.Tables!.Add(table);

            var dataSet = FormValueBinding.BuildEmptyDataSet(schema);

            Assert.Equal("test_form", dataSet.DataSetName);
            var built = Assert.IsType<DataTable>(dataSet.Tables["master"]);
            Assert.True(built.Columns.Contains("name"));
            Assert.Empty(built.Rows);
        }

        [Fact]
        [DisplayName("GetEmptyValue 對每種欄位型別都回非 null")]
        public void GetEmptyValue_KnownTypes_AreNeverNull()
        {
            Assert.Equal(string.Empty, FormValueBinding.GetEmptyValue(typeof(string)));
            Assert.Equal(Guid.Empty, FormValueBinding.GetEmptyValue(typeof(Guid)));
            Assert.Equal(DateTime.MinValue, FormValueBinding.GetEmptyValue(typeof(DateTime)));
            Assert.Equal(Array.Empty<byte>(), FormValueBinding.GetEmptyValue(typeof(byte[])));
            Assert.Equal(0, FormValueBinding.GetEmptyValue(typeof(int)));
        }
    }
}
