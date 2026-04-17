using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// 核心 DTO 的 XML / JSON 序列化 round-trip 測試。
    /// </summary>
    public class DtoSerializationTests
    {
        [Fact]
        [DisplayName("FormSchema 含主檔欄位 XML 序列化應正確還原結構")]
        public void FormSchema_XmlRoundtrip_PreservesStructure()
        {
            // Arrange
            var original = new FormSchema("Employee", "員工") { ListFields = "sys_id,sys_name" };
            var table = original.Tables!.Add("Employee", "員工");
            table.DbTableName = "ft_employee";
            table.Fields!.Add("sys_id", "員工編號", FieldDbType.String);
            table.Fields!.Add("sys_name", "員工姓名", FieldDbType.String);

            // Act
            var xml = SerializeFunc.ObjectToXml(original);
            var restored = SerializeFunc.XmlToObject<FormSchema>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("Employee", restored!.ProgId);
            Assert.Equal("員工", restored.DisplayName);
            Assert.Equal("sys_id,sys_name", restored.ListFields);
            Assert.NotNull(restored.Tables);
            Assert.Single(restored.Tables!);
            Assert.Equal("ft_employee", restored.Tables![0].DbTableName);
            Assert.Equal(2, restored.Tables[0].Fields!.Count);
        }

        [Fact]
        [DisplayName("FormSchema JSON 序列化應正確還原結構")]
        public void FormSchema_JsonRoundtrip_PreservesStructure()
        {
            // Arrange
            var original = new FormSchema("Department", "部門");
            var table = original.Tables!.Add("Department", "部門");
            table.Fields!.Add("sys_id", "部門編號", FieldDbType.String);

            // Act
            var json = SerializeFunc.ObjectToJson(original);
            var restored = SerializeFunc.JsonToObject<FormSchema>(json);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("Department", restored!.ProgId);
            Assert.Equal("部門", restored.DisplayName);
        }

        [Fact]
        [DisplayName("TableSchema 含欄位與主鍵索引 XML 序列化應正確還原")]
        public void TableSchema_XmlRoundtrip_PreservesFieldsAndIndexes()
        {
            // Arrange
            var original = new TableSchema
            {
                TableName = "ft_employee",
                DisplayName = "員工資料表"
            };
            original.Fields!.Add("sys_no", "流水號", FieldDbType.AutoIncrement);
            original.Fields!.Add(SysFields.RowId, "識別", FieldDbType.Guid);
            original.Fields!.Add("sys_id", "編號", FieldDbType.String, 20);
            original.Indexes!.AddPrimaryKey("sys_no");

            // Act
            var xml = SerializeFunc.ObjectToXml(original);
            var restored = SerializeFunc.XmlToObject<TableSchema>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("ft_employee", restored!.TableName);
            Assert.Equal(3, restored.Fields!.Count);
            Assert.Equal(20, restored.Fields!["sys_id"].Length);

            var pk = restored.GetPrimaryKey();
            Assert.NotNull(pk);
            Assert.True(pk!.PrimaryKey);
            Assert.True(pk.Unique);
        }

        [Fact]
        [DisplayName("TableSchema Clone 應深度複製欄位與索引")]
        public void TableSchema_Clone_DeepCopies()
        {
            // Arrange
            var original = new TableSchema { TableName = "ft_t", DisplayName = "T" };
            original.Fields!.Add("sys_no", "流水號", FieldDbType.AutoIncrement);
            original.Indexes!.AddPrimaryKey("sys_no");

            // Act
            var clone = original.Clone();
            clone.Fields!["sys_no"].Caption = "Modified";

            // Assert
            Assert.Equal("ft_t", clone.TableName);
            Assert.Single(clone.Fields!);
            Assert.Single(clone.Indexes!);
            Assert.Equal("流水號", original.Fields!["sys_no"].Caption);
            Assert.Equal("Modified", clone.Fields!["sys_no"].Caption);
        }

        [Fact]
        [DisplayName("FormLayout 含巢狀群組 XML 序列化應正確還原")]
        public void FormLayout_XmlRoundtrip_PreservesStructure()
        {
            // Arrange
            var original = new FormLayout
            {
                LayoutId = "EmployeeDefault",
                DisplayName = "員工預設版面"
            };
            var group = new LayoutGroup
            {
                Name = "MainGroup",
                Caption = "主要資料",
                ShowCaption = true,
                ColumnCount = 2
            };
            group.Items!.Add(new LayoutItem
            {
                FieldName = "sys_id",
                Caption = "員工編號",
                ControlType = ControlType.TextEdit
            });
            original.Groups!.Add(group);

            // Act
            var xml = SerializeFunc.ObjectToXml(original);
            var restored = SerializeFunc.XmlToObject<FormLayout>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("EmployeeDefault", restored!.LayoutId);
            Assert.Single(restored.Groups!);
            Assert.Equal("MainGroup", restored.Groups![0].Name);
            Assert.Single(restored.Groups[0].Items!);
        }

        [Fact]
        [DisplayName("FormLayout FindItem 應依欄位名稱回傳對應項目")]
        public void FormLayout_FindItem_ReturnsMatchingItem()
        {
            // Arrange
            var layout = new FormLayout { LayoutId = "Test", DisplayName = "Test" };
            var group = new LayoutGroup { Name = "G1" };
            group.Items!.Add(new LayoutItem { FieldName = "sys_id" });
            group.Items.Add(new LayoutItem { FieldName = "sys_name" });
            layout.Groups!.Add(group);

            // Act
            var found = layout.FindItem("sys_name");
            var missing = layout.FindItem("nonexistent");

            // Assert
            Assert.NotNull(found);
            Assert.Equal("sys_name", found!.FieldName);
            Assert.Null(missing);
        }

        [Fact]
        [DisplayName("DatabaseSettings XML 序列化應正確還原 Servers 與 Items")]
        public void DatabaseSettings_XmlRoundtrip_PreservesCollections()
        {
            // Arrange
            var original = new DatabaseSettings();
            original.Items!.Add(new DatabaseItem
            {
                Id = "common",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=.;Database=Test"
            });

            // Act
            var xml = SerializeFunc.ObjectToXml(original);
            var restored = SerializeFunc.XmlToObject<DatabaseSettings>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Single(restored!.Items!);
            Assert.Equal("common", restored.Items![0].Id);
            Assert.Equal(DatabaseType.SQLServer, restored.Items[0].DatabaseType);
        }

        [Fact]
        [DisplayName("DatabaseSettings Clone 應建立獨立副本")]
        public void DatabaseSettings_Clone_ProducesIndependentCopy()
        {
            // Arrange
            var original = new DatabaseSettings();
            original.Items!.Add(new DatabaseItem { Id = "common" });

            // Act
            var clone = original.Clone();
            clone.Items!.Add(new DatabaseItem { Id = "reports" });

            // Assert
            Assert.Single(original.Items!);
            Assert.Equal(2, clone.Items.Count);
        }

        [Fact]
        [DisplayName("ProgramSettings XML 序列化應正確還原")]
        public void ProgramSettings_XmlRoundtrip_Succeeds()
        {
            // Arrange
            var original = new ProgramSettings();

            // Act
            var xml = SerializeFunc.ObjectToXml(original);
            var restored = SerializeFunc.XmlToObject<ProgramSettings>(xml);

            // Assert
            Assert.NotNull(restored);
        }

        [Fact]
        [DisplayName("DbSchemaSettings XML 序列化應正確還原")]
        public void DbSchemaSettings_XmlRoundtrip_Succeeds()
        {
            // Arrange
            var original = new DbSchemaSettings();

            // Act
            var xml = SerializeFunc.ObjectToXml(original);
            var restored = SerializeFunc.XmlToObject<DbSchemaSettings>(xml);

            // Assert
            Assert.NotNull(restored);
        }

        [Fact]
        [DisplayName("SystemSettings JSON 序列化應正確還原主要屬性")]
        public void SystemSettings_JsonRoundtrip_PreservesConfiguration()
        {
            // Arrange
            var original = new SystemSettings();
            original.CommonConfiguration.Version = "4.0.1";
            original.BackendConfiguration.DatabaseId = "common";

            // Act
            var json = SerializeFunc.ObjectToJson(original);
            var restored = SerializeFunc.JsonToObject<SystemSettings>(json);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("4.0.1", restored!.CommonConfiguration.Version);
            Assert.Equal("common", restored.BackendConfiguration.DatabaseId);
        }
    }
}
