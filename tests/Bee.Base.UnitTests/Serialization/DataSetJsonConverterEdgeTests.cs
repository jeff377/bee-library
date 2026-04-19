using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Serialization
{
    /// <summary>
    /// DataSetJsonConverter 邊界與錯誤路徑測試：
    /// 涵蓋 null 輸出/輸入、非 StartObject token、未知屬性略過、
    /// tables/relations 為非陣列、relations 內參照不存在的表或欄位等分支。
    /// </summary>
    public class DataSetJsonConverterEdgeTests
    {
        private static JsonSerializerOptions Options()
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new DataSetJsonConverter());
            opts.Converters.Add(new DataTableJsonConverter());
            return opts;
        }

        [Fact]
        [DisplayName("Write 於 null DataSet 應寫出 null")]
        public void Write_NullValue_WritesNull()
        {
            var json = JsonSerializer.Serialize<DataSet?>(null, Options());
            Assert.Equal("null", json);
        }

        [Fact]
        [DisplayName("Read 於 null token 應回傳 null")]
        public void Read_NullToken_ReturnsNull()
        {
            var restored = JsonSerializer.Deserialize<DataSet?>("null", Options());
            Assert.Null(restored);
        }

        [Fact]
        [DisplayName("Read 於非 StartObject token 應拋 JsonException")]
        public void Read_NonStartObjectToken_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DataSet>("[1,2,3]", Options()));
        }

        [Fact]
        [DisplayName("Read 應略過未知的頂層屬性")]
        public void Read_UnknownTopLevelProperty_IsIgnored()
        {
            const string json = """
            {
                "dataSetName":"Named",
                "unknown":{"a":1,"b":[1,2]},
                "tables":[],
                "relations":[]
            }
            """;
            var ds = JsonSerializer.Deserialize<DataSet>(json, Options())!;
            Assert.Equal("Named", ds.DataSetName);
            Assert.Empty(ds.Tables);
            Assert.Empty(ds.Relations);
        }

        [Fact]
        [DisplayName("Read 於 tables 為非陣列時應得到空表列")]
        public void Read_TablesNotArray_YieldsNoTables()
        {
            const string json = """
            {
                "dataSetName":"X",
                "tables":123,
                "relations":[]
            }
            """;
            var ds = JsonSerializer.Deserialize<DataSet>(json, Options())!;
            Assert.Empty(ds.Tables);
        }

        [Fact]
        [DisplayName("Read 於 relations 為非陣列時應得到空關聯")]
        public void Read_RelationsNotArray_YieldsNoRelations()
        {
            const string json = """
            {
                "dataSetName":"X",
                "tables":[],
                "relations":"bad"
            }
            """;
            var ds = JsonSerializer.Deserialize<DataSet>(json, Options())!;
            Assert.Empty(ds.Relations);
        }

        [Fact]
        [DisplayName("Read relation 所參照的表不存在時應跳過該關聯")]
        public void Read_RelationReferencesMissingTable_IsSkipped()
        {
            // parentTable / childTable 指向不存在的表，BuildDataSet 中 continue
            const string json = """
            {
                "dataSetName":"X",
                "tables":[
                    {"tableName":"Only","columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],"primaryKeys":[],"rows":[]}
                ],
                "relations":[
                    {"name":"Missing","parentTable":"Ghost","childTable":"Only","parentColumns":["Id"],"childColumns":["Id"]}
                ]
            }
            """;
            var ds = JsonSerializer.Deserialize<DataSet>(json, Options())!;
            Assert.Single(ds.Tables);
            Assert.Empty(ds.Relations);
        }

        [Fact]
        [DisplayName("Read relation 欄位參照不存在時應跳過該關聯")]
        public void Read_RelationReferencesMissingColumns_IsSkipped()
        {
            // parentColumns / childColumns 對應至不存在的欄位 → 過濾後為空 → 不建立 relation
            const string json = """
            {
                "dataSetName":"X",
                "tables":[
                    {"tableName":"A","columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],"primaryKeys":[],"rows":[]},
                    {"tableName":"B","columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],"primaryKeys":[],"rows":[]}
                ],
                "relations":[
                    {"name":"BadRel","parentTable":"A","childTable":"B","parentColumns":["NoSuch"],"childColumns":["NoSuch"]}
                ]
            }
            """;
            var ds = JsonSerializer.Deserialize<DataSet>(json, Options())!;
            Assert.Equal(2, ds.Tables.Count);
            Assert.Empty(ds.Relations);
        }

        [Fact]
        [DisplayName("Read relations 元素為非 StartObject 時應略過")]
        public void Read_RelationArrayContainsNonObjects_AreSkipped()
        {
            // 陣列中包含 number 元素，應被跳過；後續正常 relation 仍能讀取
            const string json = """
            {
                "dataSetName":"X",
                "tables":[
                    {"tableName":"A","columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],"primaryKeys":[],"rows":[]},
                    {"tableName":"B","columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],"primaryKeys":[],"rows":[]}
                ],
                "relations":[
                    123,
                    {"name":"Rel","parentTable":"A","childTable":"B","parentColumns":["Id"],"childColumns":["Id"]}
                ]
            }
            """;
            var ds = JsonSerializer.Deserialize<DataSet>(json, Options())!;
            Assert.Single(ds.Relations);
            Assert.Equal("Rel", ds.Relations[0].RelationName);
        }

        [Fact]
        [DisplayName("Read relation parentColumns 為非陣列時應略過該欄位清單")]
        public void Read_RelationColumnsNotArray_YieldsEmptyColumnList()
        {
            // parentColumns 為字串 → ReadStringArray 回傳空 list → 建立 relation 失敗
            const string json = """
            {
                "dataSetName":"X",
                "tables":[
                    {"tableName":"A","columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],"primaryKeys":[],"rows":[]},
                    {"tableName":"B","columns":[{"name":"Id","type":"Integer","allowNull":true,"readOnly":false,"maxLength":-1,"caption":"","defaultValue":null}],"primaryKeys":[],"rows":[]}
                ],
                "relations":[
                    {"name":"Rel","parentTable":"A","childTable":"B","parentColumns":"not-array","childColumns":["Id"]}
                ]
            }
            """;
            var ds = JsonSerializer.Deserialize<DataSet>(json, Options())!;
            Assert.Empty(ds.Relations);
        }

        [Fact]
        [DisplayName("Write 直接呼叫 converter 於 null DataSet 應寫入 JSON null")]
        public void Write_DirectConverter_NullValue_WritesNull()
        {
            // JsonSerializer.Serialize<DataSet?>(null, ...) 會由框架短路寫入 null,
            // 不會進入 converter.Write;直接呼叫才能覆蓋 value == null 分支。
            var converter = new DataSetJsonConverter();
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, null!, new JsonSerializerOptions());
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("null", json);
        }

        [Fact]
        [DisplayName("Read 直接呼叫 converter 於 Null token 應回傳 null")]
        public void Read_DirectConverter_NullToken_ReturnsNull()
        {
            // JsonSerializer.Deserialize<DataSet?>("null", ...) 會由框架短路回傳 null,
            // 不會進入 converter.Read;直接呼叫才能覆蓋 TokenType.Null 分支。
            var converter = new DataSetJsonConverter();
            var bytes = Encoding.UTF8.GetBytes("null");
            var reader = new Utf8JsonReader(bytes);
            Assert.True(reader.Read());

            var result = converter.Read(ref reader, typeof(DataSet), new JsonSerializerOptions());
            Assert.Null(result);
        }
    }
}
