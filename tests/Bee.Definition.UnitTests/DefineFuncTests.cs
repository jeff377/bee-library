using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    public class DefineFuncTests
    {
        [Theory]
        [InlineData(DefineType.SystemSettings, typeof(SystemSettings))]
        [InlineData(DefineType.DatabaseSettings, typeof(DatabaseSettings))]
        [InlineData(DefineType.DbSchemaSettings, typeof(DbSchemaSettings))]
        [InlineData(DefineType.ProgramSettings, typeof(ProgramSettings))]
        [InlineData(DefineType.TableSchema, typeof(TableSchema))]
        [InlineData(DefineType.FormSchema, typeof(FormSchema))]
        [InlineData(DefineType.FormLayout, typeof(FormLayout))]
        [DisplayName("GetDefineType 傳入有效定義類型應回傳正確的型別")]
        public void GetDefineType_ValidType_ReturnsExpectedType(DefineType defineType, Type expectedType)
        {
            // Act
            var result = DefineFunc.GetDefineType(defineType);

            // Assert
            Assert.Equal(expectedType, result);
        }

        [Fact]
        [DisplayName("GetDefineType 傳入未支援類型應拋出 NotSupportedException")]
        public void GetDefineType_UnsupportedType_ThrowsNotSupportedException()
        {
            // Arrange
            var invalid = (DefineType)999;

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => DefineFunc.GetDefineType(invalid));
        }

        [Theory]
        [InlineData("Quantity", "N0")]
        [InlineData("UnitPrice", "N2")]
        [InlineData("Amount", "N2")]
        [InlineData("Cost", "N4")]
        [InlineData("quantity", "N0")]
        [DisplayName("GetNumberFormatString 已知格式名稱應回傳對應格式字串")]
        public void GetNumberFormatString_KnownFormat_ReturnsExpectedString(string numberFormat, string expected)
        {
            // Act
            var result = DefineFunc.GetNumberFormatString(numberFormat);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Unknown")]
        [InlineData(null)]
        [DisplayName("GetNumberFormatString 空或未知格式應回傳空字串")]
        public void GetNumberFormatString_EmptyOrUnknown_ReturnsEmpty(string? numberFormat)
        {
            // Act
            var result = DefineFunc.GetNumberFormatString(numberFormat!);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("GetListLayout 應包含隱藏的 RowID 欄位並依 ListFields 加入顯示欄")]
        public void GetListLayout_ValidSchema_ContainsRowIdAndListFields()
        {
            // Arrange
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_id,sys_name" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields!.Add(new FormField("sys_id", "編號", FieldDbType.String) { Width = 150 });
            table.Fields!.Add(new FormField("sys_name", "名稱", FieldDbType.String));

            // Act
            var grid = schema.GetListLayout();

            // Assert
            Assert.Equal("Demo", grid.TableName);
            Assert.Equal(3, grid.Columns!.Count);

            var rowIdColumn = grid.Columns[0];
            Assert.Equal(SysFields.RowId, rowIdColumn.FieldName);
            Assert.False(rowIdColumn.Visible);

            var idColumn = grid.Columns[1];
            Assert.Equal("sys_id", idColumn.FieldName);
            Assert.Equal(150, idColumn.Width);

            var nameColumn = grid.Columns[2];
            Assert.Equal("sys_name", nameColumn.FieldName);
            Assert.Equal(120, nameColumn.Width);
        }
    }
}
