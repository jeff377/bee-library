using System;
using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Serialization
{
    /// <summary>
    /// DataTableJsonConverter 邊界與錯誤路徑測試：
    /// 涵蓋非預期 JSON token、各種 primitive/非 primitive 值、
    /// ConvertValue 的型別轉換分支，以及 Read/Write 的 null 處理。
    /// </summary>
    public class DataTableJsonConverterEdgeTests
    {
        private static JsonSerializerOptions Options()
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new DataTableJsonConverter());
            return opts;
        }

        [Fact]
        [DisplayName("Read 於非 StartObject token 應拋 JsonException")]
        public void Read_NonStartObjectToken_Throws()
        {
            // JSON 陣列不應該被當成 DataTable 讀
            const string json = "[1,2,3]";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DataTable>(json, Options()));
        }

        [Fact]
        [DisplayName("Read 於 null token 應回傳 null")]
        public void Read_NullToken_ReturnsNull()
        {
            var restored = JsonSerializer.Deserialize<DataTable?>("null", Options());
            Assert.Null(restored);
        }

        [Fact]
        [DisplayName("Write 於 null DataTable 應寫出 null")]
        public void Write_NullValue_WritesNull()
        {
            var json = JsonSerializer.Serialize<DataTable?>(null, Options());
            Assert.Equal("null", json);
        }

        [Fact]
        [DisplayName("Read 應略過未知的頂層屬性")]
        public void Read_UnknownTopLevelProperty_IsIgnored()
        {
            // 包含 unknown 屬性，應透過 default: reader.Skip() 略過
            const string json = """
            {
                "tableName":"T",
                "unknown":{"nested":"value"},
                "columns":[],
                "primaryKeys":[],
                "rows":[]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.Equal("T", dt.TableName);
        }

        [Fact]
        [DisplayName("Read 於 columns 為非陣列時應得到空欄位")]
        public void Read_ColumnsNotArray_YieldsNoColumns()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":123,
                "primaryKeys":[],
                "rows":[]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.Empty(dt.Columns);
        }

        [Fact]
        [DisplayName("Read 於 primaryKeys 為非陣列時應不拋且無主鍵")]
        public void Read_PrimaryKeysNotArray_IsSafe()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"Id","defaultValue":null}],
                "primaryKeys":"bad",
                "rows":[]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.Empty(dt.PrimaryKey);
        }

        [Fact]
        [DisplayName("Read 於 rows 為非陣列時應得到空列")]
        public void Read_RowsNotArray_YieldsNoRows()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"Id","defaultValue":null}],
                "primaryKeys":[],
                "rows":null
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.Empty(dt.Rows);
        }

        [Fact]
        [DisplayName("Read 應略過資料列中未知的屬性")]
        public void Read_UnknownRowProperty_IsSkipped()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"Id","defaultValue":null}],
                "primaryKeys":[],
                "rows":[{"state":"Added","current":{"Id":1},"extra":{"nested":1}}]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal(1, dt.Rows[0]["Id"]);
        }

        [Fact]
        [DisplayName("Read defaultValue 為 primitive 時應設定欄位預設值")]
        public void Read_ColumnWithPrimitiveDefault_UsesDefault()
        {
            // String 預設值
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Name","type":"String","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":"hello"}],
                "primaryKeys":[],
                "rows":[]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.Equal("hello", dt.Columns["Name"]!.DefaultValue);
        }

        [Fact]
        [DisplayName("Read 數值型 column 於 current 值為 null 時應回傳 DBNull")]
        public void Read_NullValueInCurrent_IsDbNull()
        {
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Age","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],
                "primaryKeys":[],
                "rows":[{"state":"Added","current":{"Age":null}}]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.True(dt.Rows[0].IsNull("Age"));
        }

        [Fact]
        [DisplayName("ConvertValue 於 byte[] 目標型別 + Base64 字串應回傳原始 bytes")]
        public void ConvertValue_ByteArrayFromBase64_ReturnsBytes()
        {
            var bytes = new byte[] { 0x01, 0x02, 0xAB, 0xFF };
            var base64 = Convert.ToBase64String(bytes);

            var result = DataTableJsonConverter.ConvertValue(base64, typeof(byte[]));

            var typed = Assert.IsType<byte[]>(result);
            Assert.Equal(bytes, typed);
        }

        [Fact]
        [DisplayName("ConvertValue 於 byte[] 目標型別 + 非字串輸入應原樣回傳")]
        public void ConvertValue_ByteArrayNonString_ReturnsSame()
        {
            var input = 123L;
            var result = DataTableJsonConverter.ConvertValue(input, typeof(byte[]));
            Assert.Equal(input, result);
        }

        [Fact]
        [DisplayName("ConvertValue 於 Guid 目標型別 + 字串應 Parse 為 Guid")]
        public void ConvertValue_GuidFromString_ReturnsGuid()
        {
            var guid = Guid.NewGuid();
            var result = DataTableJsonConverter.ConvertValue(guid.ToString(), typeof(Guid));
            Assert.Equal(guid, result);
        }

        [Fact]
        [DisplayName("ConvertValue 於 Guid 目標型別 + 非字串應原樣回傳")]
        public void ConvertValue_GuidNonString_ReturnsSame()
        {
            var result = DataTableJsonConverter.ConvertValue(42L, typeof(Guid));
            Assert.Equal(42L, result);
        }

        [Fact]
        [DisplayName("ConvertValue 於 DateTime 目標型別 + DateTime 值應直接回傳")]
        public void ConvertValue_DateTimeFromDateTime_ReturnsSame()
        {
            var dt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var result = DataTableJsonConverter.ConvertValue(dt, typeof(DateTime));
            Assert.Equal(dt, result);
        }

        [Fact]
        [DisplayName("ConvertValue 於 DateTime 目標型別 + 字串應 Parse")]
        public void ConvertValue_DateTimeFromString_ReturnsParsed()
        {
            var result = DataTableJsonConverter.ConvertValue("2026-04-17T08:30:00", typeof(DateTime));
            var typed = Assert.IsType<DateTime>(result);
            Assert.Equal(new DateTime(2026, 4, 17, 8, 30, 0), typed);
        }

        [Fact]
        [DisplayName("ConvertValue 於 DateTime 目標型別 + long 輸入無法轉換應原樣回傳")]
        public void ConvertValue_DateTimeFromLong_ReturnsSameAsFallback()
        {
            // long 無法轉 DateTime：先進到 DateTime 分支，兩個 if 都不 match 後原樣回傳
            var result = DataTableJsonConverter.ConvertValue(12345L, typeof(DateTime));
            Assert.Equal(12345L, result);
        }

        [Fact]
        [DisplayName("ConvertValue 於數值型別應使用 Convert.ChangeType 轉換")]
        public void ConvertValue_LongToInt_Converts()
        {
            var result = DataTableJsonConverter.ConvertValue(42L, typeof(int));
            Assert.Equal(42, result);
            Assert.IsType<int>(result);
        }

        [Fact]
        [DisplayName("ConvertValue 於無法轉換的組合應走 catch 原樣回傳")]
        public void ConvertValue_IncompatibleType_ReturnsSameOnCatch()
        {
            // 將物件陣列轉為 int：Convert.ChangeType 會 throw → catch → 原樣回傳
            var input = new object();
            var result = DataTableJsonConverter.ConvertValue(input, typeof(int));
            Assert.Same(input, result);
        }

        [Fact]
        [DisplayName("ReadPrimitiveValue 經由 true/false/null/複雜 token 應正確回傳")]
        public void Read_BooleanAndComplexTokens_HandledCorrectly()
        {
            // 故意用 String 欄位（typeLookup 會走 ConvertValue 分支）
            const string json = """
            {
                "tableName":"T",
                "columns":[
                    {"name":"A","type":"Boolean","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null},
                    {"name":"B","type":"Boolean","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null},
                    {"name":"C","type":"String","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}
                ],
                "primaryKeys":[],
                "rows":[{"state":"Added","current":{"A":true,"B":false,"C":{"complex":"object"}}}]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            Assert.Equal(true, dt.Rows[0]["A"]);
            Assert.Equal(false, dt.Rows[0]["B"]);
            // 複雜 token 會被 Skip 回傳 null → DBNull
            Assert.True(dt.Rows[0].IsNull("C"));
        }

        [Fact]
        [DisplayName("Read 數值超出 long 範圍應 fallback 至 double")]
        public void Read_NumberBeyondLong_FallsBackToDouble()
        {
            // String 欄位 + 數字值：TryGetInt64 失敗 → GetDouble
            const string json = """
            {
                "tableName":"T",
                "columns":[{"name":"Big","type":"String","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],
                "primaryKeys":[],
                "rows":[{"state":"Added","current":{"Big":1.5e20}}]
            }
            """;
            var dt = JsonSerializer.Deserialize<DataTable>(json, Options())!;
            // 值會被轉為 String（透過 Convert.ChangeType）
            Assert.NotNull(dt.Rows[0]["Big"]);
        }
    }
}
