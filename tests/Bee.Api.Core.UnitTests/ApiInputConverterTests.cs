using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Bee.Api.Core.Conversion;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiInputConverter.Convert 的分支覆蓋：null 來源、型別相容、JsonElement
    /// 反序列化、介面/抽象型別直接回傳、屬性複製與型別不符略過。
    /// </summary>
    public class ApiInputConverterTests
    {
        public class SourceDto
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Extra { get; set; } = string.Empty;
        }

        public class TargetDto
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string ReadOnly { get; } = "init";
        }

        public interface IMarker { }

        public abstract class AbstractBase
        {
            public string Title { get; set; } = string.Empty;
        }

        public class ConcreteMarker : IMarker
        {
            public string Name { get; set; } = string.Empty;
        }

        public class TypeMismatchTarget
        {
            public int Name { get; set; }
        }

        [Fact]
        [DisplayName("Convert 於 null 來源應回傳 null")]
        public void Convert_NullSource_ReturnsNull()
        {
            var result = ApiInputConverter.Convert(null!, typeof(TargetDto));
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Convert 於 source 已相容 target 型別時應原樣回傳")]
        public void Convert_SourceAssignableToTarget_ReturnsSame()
        {
            var source = new TargetDto { Name = "x", Age = 1 };
            var result = ApiInputConverter.Convert(source, typeof(TargetDto));
            Assert.Same(source, result);
        }

        [Fact]
        [DisplayName("Convert 於目標為介面型別應回傳原 source")]
        public void Convert_TargetIsInterface_ReturnsSource()
        {
            // source 不實作 IMarker → 不觸發 IsInstanceOfType；但目標為 interface → 直接回傳
            var source = new SourceDto { Name = "x" };
            var result = ApiInputConverter.Convert(source, typeof(IMarker));
            Assert.Same(source, result);
        }

        [Fact]
        [DisplayName("Convert 於目標為抽象型別應回傳原 source")]
        public void Convert_TargetIsAbstract_ReturnsSource()
        {
            var source = new SourceDto { Name = "x" };
            var result = ApiInputConverter.Convert(source, typeof(AbstractBase));
            Assert.Same(source, result);
        }

        [Fact]
        [DisplayName("Convert 應複製同名且型別相容的公開屬性至目標實例")]
        public void Convert_CopiesMatchingPropertiesToNewTarget()
        {
            var source = new SourceDto { Name = "Alice", Age = 30, Extra = "Z" };
            var result = ApiInputConverter.Convert(source, typeof(TargetDto));

            var target = Assert.IsType<TargetDto>(result);
            Assert.NotSame(source, target);
            Assert.Equal("Alice", target.Name);
            Assert.Equal(30, target.Age);
        }

        [Fact]
        [DisplayName("Convert 應略過目標型別與 source 不相容的屬性")]
        public void Convert_SkipsPropertiesWithIncompatibleTypes()
        {
            var source = new SourceDto { Name = "Alice" };
            var result = ApiInputConverter.Convert(source, typeof(TypeMismatchTarget));

            var target = Assert.IsType<TypeMismatchTarget>(result);
            // source.Name 為 string，target.Name 為 int → 略過，保留 int 預設 0
            Assert.Equal(0, target.Name);
        }

        [Fact]
        [DisplayName("Convert 於 JsonElement 來源應以 camelCase 大小寫不敏感反序列化")]
        public void Convert_JsonElement_DeserializesCaseInsensitive()
        {
            var json = """{"name":"Bob","age":42}""";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement.Clone();

            var result = ApiInputConverter.Convert(element, typeof(TargetDto));

            var target = Assert.IsType<TargetDto>(result);
            Assert.Equal("Bob", target.Name);
            Assert.Equal(42, target.Age);
        }

        public class DataSetCarrier
        {
            public DataSet? DataSet { get; set; }
        }

        [Fact]
        [DisplayName("Convert 於 JsonElement 來源應透過 DataTable converter 還原 RowState 與欄位值")]
        public void Convert_JsonElement_DeserializesDataSetWithRowState()
        {
            // Regression: ApiInputConverter must include DataSet/DataTable/StringEnum
            // converters, otherwise Plain-format requests carrying a DataSet payload
            // silently deserialize to empty rows (Save sees "no pending changes").
            const string json = """
                {
                    "dataSet": {
                        "dataSetName": "Test",
                        "tables": [{
                            "tableName": "T",
                            "columns": [{
                                "name": "X",
                                "type": "String",
                                "allowNull": false,
                                "readOnly": false,
                                "maxLength": -1,
                                "caption": "X",
                                "defaultValue": ""
                            }],
                            "primaryKeys": [],
                            "rows": [{
                                "state": "Added",
                                "current": { "X": "v" }
                            }]
                        }],
                        "relations": []
                    }
                }
                """;
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement.Clone();

            var result = ApiInputConverter.Convert(element, typeof(DataSetCarrier));

            var carrier = Assert.IsType<DataSetCarrier>(result);
            Assert.NotNull(carrier.DataSet);
            Assert.Single(carrier.DataSet.Tables);
            var table = carrier.DataSet.Tables[0];
            Assert.Equal("T", table.TableName);
            Assert.Single(table.Rows);
            Assert.Equal(DataRowState.Added, table.Rows[0].RowState);
            Assert.Equal("v", table.Rows[0]["X"]);
        }

        [Fact]
        [DisplayName("Convert 於 JsonElement 來源應支援 PascalCase 屬性名")]
        public void Convert_JsonElement_AcceptsPascalCase()
        {
            var json = """{"Name":"Cathy","Age":7}""";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement.Clone();

            var result = ApiInputConverter.Convert(element, typeof(TargetDto));

            var target = Assert.IsType<TargetDto>(result);
            Assert.Equal("Cathy", target.Name);
            Assert.Equal(7, target.Age);
        }
    }
}
