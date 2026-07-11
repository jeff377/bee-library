using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Serialization
{
    /// <summary>
    /// DataTableJsonConverter 覆蓋率補強測試：直接呼叫 Write/Read 的 null 分支、
    /// 涵蓋所有 FieldDbType 欄位型別的完整 round-trip（Added/Modified/Deleted/Unchanged 各列狀態、
    /// null 值、original 值），以及 ReadColumnField / ReadValueMap / SetRowValues 的邊界分支。
    /// </summary>
    public class DataTableJsonConverterCoverageTests
    {
        private static JsonSerializerOptions Options()
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new DataTableJsonConverter());
            return opts;
        }

        private static readonly byte[] s_sampleBytes = { 0x01, 0x02, 0x03, 0xFF };
        private static readonly Guid s_sampleGuid = new("11112222-3333-4444-5555-666677778888");
        private static readonly DateTime s_sampleDate =
            new(2026, 4, 17, 8, 30, 0, DateTimeKind.Unspecified);

        // ---- 直接呼叫 Write / Read 的 null 分支（STJ 頂層 null 不會經過 converter）----

        [Fact]
        [DisplayName("Write 直接以 null DataTable 呼叫應寫出 null 字面值")]
        public void Write_NullValueDirectInvoke_WritesNull()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                new DataTableJsonConverter().Write(writer, null!, Options());
            }

            var result = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("null", result);
        }

        [Fact]
        [DisplayName("Read 直接以 null token 呼叫應回傳 null")]
        public void Read_NullTokenDirectInvoke_ReturnsNull()
        {
            var bytes = Encoding.UTF8.GetBytes("null");
            var reader = new Utf8JsonReader(bytes);
            reader.Read(); // 移到 Null token

            var converter = new DataTableJsonConverter();
            var result = converter.Read(ref reader, typeof(DataTable), Options());

            Assert.Null(result);
        }

        // ---- 涵蓋所有 FieldDbType 欄位型別 + 全列狀態的完整 round-trip ----

        private static DataTable BuildAllTypesTable()
        {
            var dt = new DataTable("Full");
            var strCol = new DataColumn("Str", typeof(string)) { AllowDBNull = true, Caption = "文字" };
            var flagCol = new DataColumn("Flag", typeof(bool)) { AllowDBNull = true };
            var shCol = new DataColumn("Sh", typeof(short)) { AllowDBNull = true };
            var intCol = new DataColumn("Int", typeof(int)) { AllowDBNull = false };
            var lngCol = new DataColumn("Lng", typeof(long)) { AllowDBNull = true };
            var decCol = new DataColumn("Dec", typeof(decimal)) { AllowDBNull = true };
            var dtCol = new DataColumn("Dt", typeof(DateTime)) { AllowDBNull = true };
            var gdCol = new DataColumn("Gd", typeof(Guid)) { AllowDBNull = true };
            var binCol = new DataColumn("Bin", typeof(byte[])) { AllowDBNull = true };

            dt.Columns.Add(strCol);
            dt.Columns.Add(flagCol);
            dt.Columns.Add(shCol);
            dt.Columns.Add(intCol);
            dt.Columns.Add(lngCol);
            dt.Columns.Add(decCol);
            dt.Columns.Add(dtCol);
            dt.Columns.Add(gdCol);
            dt.Columns.Add(binCol);
            dt.PrimaryKey = new[] { intCol };
            return dt;
        }

        private static void FillRow(DataRow row, string str, int id, bool flag)
        {
            row["Str"] = str;
            row["Flag"] = flag;
            row["Sh"] = (short)7;
            row["Int"] = id;
            row["Lng"] = 9_000_000_000L;
            row["Dec"] = 123.45m;
            row["Dt"] = s_sampleDate;
            row["Gd"] = s_sampleGuid;
            row["Bin"] = s_sampleBytes;
        }

        [Fact]
        [DisplayName("所有 FieldDbType 欄位 + 各列狀態的完整 round-trip 應保留型別與值")]
        public void WriteRead_AllTypesAndRowStates_RoundTrips()
        {
            var dt = BuildAllTypesTable();

            var r0 = dt.NewRow(); FillRow(r0, "Keep", 1, true); dt.Rows.Add(r0);
            var r1 = dt.NewRow(); FillRow(r1, "Before", 2, false); dt.Rows.Add(r1);
            var r2 = dt.NewRow(); FillRow(r2, "Doomed", 3, true); dt.Rows.Add(r2);
            dt.AcceptChanges(); // r0/r1/r2 → Unchanged

            dt.Rows[1]["Str"] = "After"; // r1 → Modified
            dt.Rows[2].Delete();         // r2 → Deleted

            var r3 = dt.NewRow();
            FillRow(r3, "Fresh", 4, false);
            r3["Lng"] = DBNull.Value; // 帶 null 值 → WriteRowValues null 分支
            dt.Rows.Add(r3);          // r3 → Added

            var json = JsonSerializer.Serialize(dt, Options());
            var restored = JsonSerializer.Deserialize<DataTable>(json, Options())!;

            Assert.Equal("Full", restored.TableName);
            Assert.Equal(9, restored.Columns.Count);
            Assert.Equal(4, restored.Rows.Count);
            Assert.Single(restored.PrimaryKey);
            Assert.Equal("Int", restored.PrimaryKey[0].ColumnName);

            // r0 Unchanged
            Assert.Equal(DataRowState.Unchanged, restored.Rows[0].RowState);
            Assert.Equal("Keep", restored.Rows[0]["Str"]);
            Assert.Equal((short)7, restored.Rows[0]["Sh"]);
            Assert.Equal(9_000_000_000L, restored.Rows[0]["Lng"]);
            Assert.Equal(123.45m, restored.Rows[0]["Dec"]);
            Assert.Equal(s_sampleDate, restored.Rows[0]["Dt"]);
            Assert.Equal(s_sampleGuid, restored.Rows[0]["Gd"]);
            Assert.Equal(s_sampleBytes, (byte[])restored.Rows[0]["Bin"]);
            Assert.True((bool)restored.Rows[0]["Flag"]);

            // r1 Modified：current/original 皆還原
            Assert.Equal(DataRowState.Modified, restored.Rows[1].RowState);
            Assert.Equal("After", restored.Rows[1]["Str", DataRowVersion.Current]);
            Assert.Equal("Before", restored.Rows[1]["Str", DataRowVersion.Original]);

            // r2 Deleted：僅 original
            Assert.Equal(DataRowState.Deleted, restored.Rows[2].RowState);
            Assert.Equal("Doomed", restored.Rows[2]["Str", DataRowVersion.Original]);

            // r3 Added，且帶 null 值
            Assert.Equal(DataRowState.Added, restored.Rows[3].RowState);
            Assert.Equal("Fresh", restored.Rows[3]["Str"]);
            Assert.True(restored.Rows[3].IsNull("Lng"));
        }

        // ---- ReadColumnField 的 null-coalescing fallback 分支 ----

        [Fact]
        [DisplayName("Read 欄位 name/type/caption 為 null 時應套用 fallback 預設值")]
        public void ReadColumns_NullNameTypeCaption_UsesFallbacks()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":null,"type":null,"allowNull":true,"readOnly":false,"maxLength":-1,"caption":null,"defaultValue":null}],
                "primaryKeys":[],
                "rows":[]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;

            // type null → fallback "String"；欄位仍建立成功
            Assert.Single(dt.Columns);
            Assert.Equal(typeof(string), dt.Columns[0].DataType);
        }

        // ---- ReadValueMap 於 value 非 StartObject 的防護分支（279/280）----

        [Fact]
        [DisplayName("Read 於 current 非物件時應得到無值的列且不拋例外")]
        public void ReadRows_CurrentNotObject_YieldsRowWithoutValues()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],
                "primaryKeys":[],
                "rows":[{"state":"Added","current":123}]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;

            Assert.Equal(1, dt.Rows.Count);
            Assert.True(dt.Rows[0].IsNull("Id"));
        }

        // ---- SetRowValues 於 values 為 null 的提前返回（449）----

        [Fact]
        [DisplayName("Read Added 列缺少 current 區段時應建立無值列（SetRowValues null 分支）")]
        public void ReadRows_AddedRowWithoutCurrent_BuildsEmptyRow()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],
                "primaryKeys":[],
                "rows":[{"state":"Added"}]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;

            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal(DataRowState.Added, dt.Rows[0].RowState);
        }

        // ---- ReadPrimitiveValue：字串為日期時走 TryGetDateTime 分支（309/313-314）----

        [Fact]
        [DisplayName("Read String 欄位存放日期字串時應經 TryGetDateTime 解析")]
        public void ReadRows_DateLikeStringInStringColumn_ParsedViaDateTimeBranch()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"When","type":"DateTime","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],
                "primaryKeys":[],
                "rows":[{"state":"Added","current":{"When":"2026-04-17T08:30:00"}}]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;

            Assert.Equal(new DateTime(2026, 4, 17, 8, 30, 0), dt.Rows[0]["When"]);
        }
    }
}
