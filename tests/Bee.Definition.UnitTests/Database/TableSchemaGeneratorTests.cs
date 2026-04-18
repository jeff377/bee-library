using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Database
{
    /// <summary>
    /// TableSchemaGenerator 將 FormTable 轉換為 TableSchema 的測試。
    /// </summary>
    public class TableSchemaGeneratorTests
    {
        [Fact]
        [DisplayName("Generate 傳入 null 應拋出 ArgumentNullException")]
        public void Generate_NullFormTable_ThrowsArgumentNullException()
        {
            // Arrange

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TableSchemaGenerator.Generate(null!));
        }

        [Fact]
        [DisplayName("Generate 有 DbTableName 時應使用 DbTableName 作為表名")]
        public void Generate_DbTableNameSpecified_UsesDbTableName()
        {
            // Arrange
            var formTable = BuildFormTable(dbTableName: "ft_employee");

            // Act
            var schema = TableSchemaGenerator.Generate(formTable);

            // Assert
            Assert.Equal("ft_employee", schema.TableName);
        }

        [Fact]
        [DisplayName("Generate 無 DbTableName 時應使用 TableName")]
        public void Generate_NoDbTableName_UsesTableName()
        {
            // Arrange
            var formTable = BuildFormTable(dbTableName: string.Empty);

            // Act
            var schema = TableSchemaGenerator.Generate(formTable);

            // Assert
            Assert.Equal("Employee", schema.TableName);
        }

        [Fact]
        [DisplayName("Generate 應僅加入 DbField 類型欄位，忽略其他欄位類型")]
        public void Generate_OnlyAddsDbFields()
        {
            // Arrange
            var formTable = BuildFormTable();
            // 加入非 DbField 類型的欄位，應被忽略
            formTable.Fields!.Add(new FormField("virtual_field", "虛擬欄位", FieldDbType.String, FieldType.RelationField));

            // Act
            var schema = TableSchemaGenerator.Generate(formTable);

            // Assert
            Assert.DoesNotContain(schema.Fields!, f => f.FieldName == "virtual_field");
        }

        [Fact]
        [DisplayName("Generate 應自動加入主鍵索引於 sys_no")]
        public void Generate_AddsPrimaryKeyIndexOnSysNo()
        {
            // Arrange
            var formTable = BuildFormTable();

            // Act
            var schema = TableSchemaGenerator.Generate(formTable);

            // Assert
            var pk = schema.GetPrimaryKey();
            Assert.NotNull(pk);
            Assert.True(pk!.PrimaryKey);
            Assert.True(pk.Unique);
        }

        [Fact]
        [DisplayName("Generate 應自動加入 sys_rowid 唯一索引")]
        public void Generate_AddsUniqueIndexOnRowId()
        {
            // Arrange
            var formTable = BuildFormTable();

            // Act
            var schema = TableSchemaGenerator.Generate(formTable);

            // Assert
            Assert.Contains(schema.Indexes!, idx =>
                idx.IndexFields!.Contains(SysFields.RowId) && idx.Unique && !idx.PrimaryKey);
        }

        [Fact]
        [DisplayName("Generate 欄位 MaxLength 應對應至 DbField.Length")]
        public void Generate_MapsMaxLengthToDbFieldLength()
        {
            // Arrange
            var formTable = new FormTable("Demo", "示範");
            formTable.Fields!.Add(new FormField("name", "名稱", FieldDbType.String) { MaxLength = 50 });

            // Act
            var schema = TableSchemaGenerator.Generate(formTable);

            // Assert
            Assert.Equal(50, schema.Fields!["name"].Length);
        }

        [Fact]
        [DisplayName("Generate 有 RelationProgId 的欄位應加入外鍵索引")]
        public void Generate_FieldWithRelationProgId_AddsForeignKeyIndex()
        {
            // Arrange
            var formTable = BuildFormTable();
            formTable.Fields!.Add(new FormField("dept_rowid", "部門", FieldDbType.String)
            {
                RelationProgId = "Department"
            });

            // Act
            var schema = TableSchemaGenerator.Generate(formTable);

            // Assert
            Assert.Contains(schema.Indexes!, idx => idx.IndexFields!.Contains("dept_rowid") && !idx.Unique);
        }

        private static FormTable BuildFormTable(string dbTableName = "ft_employee")
        {
            var formTable = new FormTable("Employee", "員工") { DbTableName = dbTableName };
            formTable.Fields!.Add(SysFields.No, "流水號", FieldDbType.AutoIncrement);
            formTable.Fields!.Add(SysFields.RowId, "識別", FieldDbType.Guid);
            formTable.Fields!.Add(SysFields.Id, "編號", FieldDbType.String);
            formTable.Fields!.Add(SysFields.Name, "名稱", FieldDbType.String);
            return formTable;
        }
    }
}
