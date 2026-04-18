using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// FormTable 未覆蓋路徑測試：ToString、GenerateDbTable、
    /// RelationFieldReferences 的 KeyNotFoundException/InvalidOperationException 分支。
    /// </summary>
    public class FormTableTests
    {
        private static FormTable BuildBasic(string tableName = "Employee", string displayName = "員工")
        {
            var ft = new FormTable(tableName, displayName);
            ft.Fields!.Add(new FormField("sys_no", "流水號", FieldDbType.AutoIncrement));
            ft.Fields!.Add(new FormField("sys_rowid", "識別", FieldDbType.Guid));
            ft.Fields!.Add(new FormField("name", "名稱", FieldDbType.String));
            return ft;
        }

        [Fact]
        [DisplayName("ToString 應回傳 TableName 與 DisplayName 的串接")]
        public void ToString_ReturnsTableNameDashDisplayName()
        {
            var ft = new FormTable("Customer", "客戶");
            Assert.Equal("Customer - 客戶", ft.ToString());
        }

        [Fact]
        [DisplayName("GenerateDbTable 應透過 TableSchemaGenerator 建立 TableSchema")]
        public void GenerateDbTable_DelegatesToSchemaGenerator()
        {
            var ft = BuildBasic();
            ft.DbTableName = "ft_employee";

            var schema = ft.GenerateDbTable();

            Assert.NotNull(schema);
            Assert.Equal("ft_employee", schema.TableName);
        }

        [Fact]
        [DisplayName("RelationFieldReferences 對 DestinationField 不存在於 Fields 應拋 KeyNotFoundException")]
        public void RelationFieldReferences_MissingDestinationField_Throws()
        {
            var ft = BuildBasic();
            // 帶 RelationProgId 的 DbField,mapping 的 DestinationField 指向不存在欄位
            var rel = new FormField("dept_rowid", "部門", FieldDbType.String)
            {
                RelationProgId = "Department"
            };
            rel.RelationFieldMappings!.Add(new FieldMapping("dept_name", "ghost_field"));
            ft.Fields!.Add(rel);

            Assert.Throws<KeyNotFoundException>(() => _ = ft.RelationFieldReferences);
        }

        [Fact]
        [DisplayName("RelationFieldReferences 對重複 DestinationField 應拋 InvalidOperationException")]
        public void RelationFieldReferences_DuplicateDestinationField_Throws()
        {
            var ft = BuildBasic();
            // 兩個不同的關聯欄位,mapping 都寫入同一個 DestinationField → 觸發 seen.Add 失敗
            var rel1 = new FormField("dept_rowid", "部門", FieldDbType.String)
            {
                RelationProgId = "Department"
            };
            rel1.RelationFieldMappings!.Add(new FieldMapping("dept_name", "name"));

            var rel2 = new FormField("dept2_rowid", "部門2", FieldDbType.String)
            {
                RelationProgId = "Department"
            };
            rel2.RelationFieldMappings!.Add(new FieldMapping("dept2_name", "name"));

            ft.Fields!.Add(rel1);
            ft.Fields!.Add(rel2);

            Assert.Throws<InvalidOperationException>(() => _ = ft.RelationFieldReferences);
        }

        [Fact]
        [DisplayName("RelationFieldReferences 對合法 mapping 應建立對應引用")]
        public void RelationFieldReferences_ValidMapping_BuildsReference()
        {
            var ft = BuildBasic();
            var rel = new FormField("dept_rowid", "部門", FieldDbType.String)
            {
                RelationProgId = "Department"
            };
            rel.RelationFieldMappings!.Add(new FieldMapping("dept_name", "name"));
            ft.Fields!.Add(rel);

            var refs = ft.RelationFieldReferences;

            Assert.Single(refs);
            Assert.Equal("name", refs[0].FieldName);
        }
    }
}
