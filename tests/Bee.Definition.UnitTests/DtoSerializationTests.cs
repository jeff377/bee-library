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
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<FormSchema>(xml);

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
            var json = JsonCodec.Serialize(original);
            var restored = JsonCodec.Deserialize<FormSchema>(json);

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
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<TableSchema>(xml);

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
        [DisplayName("FormLayout 含 Sections 與 Details XML 序列化應正確還原結構")]
        public void FormLayout_FullStructure_XmlRoundtrip()
        {
            // Arrange
            var original = new FormLayout
            {
                LayoutId = "default",
                ProgId = "Employee",
                Caption = "員工資料",
                ColumnCount = 3
            };
            var section = new LayoutSection
            {
                Name = "Main",
                Caption = "基本資料"
            };
            section.Fields!.Add(new LayoutField
            {
                FieldName = "sys_id",
                Caption = "員工編號",
                ControlType = ControlType.TextEdit
            });
            section.Fields!.Add(new LayoutField
            {
                FieldName = "memo",
                Caption = "備註",
                ControlType = ControlType.MemoEdit,
                RowSpan = 3,
                ColumnSpan = 2
            });
            original.Sections!.Add(section);

            var grid = new LayoutGrid("emp_skill", "專長");
            grid.Columns!.Add(new LayoutColumn
            {
                FieldName = "skill_name",
                Caption = "專長名稱"
            });
            grid.Columns!.Add(new LayoutColumn
            {
                FieldName = "proficiency",
                Caption = "熟練度",
                Width = 80
            });
            grid.Columns!.Add(new LayoutColumn
            {
                FieldName = SysFields.RowId,
                Caption = "Row ID",
                Visible = false
            });
            original.Details!.Add(grid);

            // Act
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<FormLayout>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("default", restored!.LayoutId);
            Assert.Equal("Employee", restored.ProgId);
            Assert.Equal("員工資料", restored.Caption);
            Assert.Equal(3, restored.ColumnCount);

            Assert.Single(restored.Sections!);
            Assert.Equal("Main", restored.Sections![0].Name);
            Assert.Equal(2, restored.Sections![0].Fields!.Count);
            var memo = restored.Sections![0].Fields![1];
            Assert.Equal(ControlType.MemoEdit, memo.ControlType);
            Assert.Equal(3, memo.RowSpan);
            Assert.Equal(2, memo.ColumnSpan);

            Assert.Single(restored.Details!);
            var restoredGrid = restored.Details![0];
            Assert.Equal("emp_skill", restoredGrid.TableName);
            Assert.Equal(3, restoredGrid.Columns!.Count);
            Assert.Equal(80, restoredGrid.Columns![1].Width);
            Assert.False(restoredGrid.Columns![2].Visible);
        }

        [Fact]
        [DisplayName("LayoutColumn Width=0 序列化時應省略 Width 屬性")]
        public void LayoutColumn_WidthZero_OmitsXmlAttribute()
        {
            // Arrange
            var column = new LayoutColumn { FieldName = "col", Caption = "欄" };

            // Act
            var xml = XmlCodec.Serialize(column);

            // Assert
            Assert.DoesNotContain("Width=", xml);
        }

        [Fact]
        [DisplayName("LayoutColumn Width 有值時 round-trip 應保留")]
        public void LayoutColumn_NonZeroWidth_PreservesValue()
        {
            // Arrange
            var original = new LayoutColumn { FieldName = "col", Caption = "欄", Width = 150 };

            // Act
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<LayoutColumn>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(150, restored!.Width);
        }

        [Fact]
        [DisplayName("FormField ListItems XML 序列化應正確還原")]
        public void FormField_ListItems_XmlRoundtrip()
        {
            // Arrange
            var original = new FormField("status", "狀態", FieldDbType.String);
            original.ListItems!.Add("A", "啟用");
            original.ListItems!.Add("D", "停用");

            // Act
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<FormField>(xml);

            // Assert
            Assert.NotNull(restored);
            Assert.NotNull(restored!.ListItems);
            Assert.Equal(2, restored.ListItems!.Count);
            Assert.Equal("啟用", restored.ListItems!["A"].Text);
            Assert.Equal("停用", restored.ListItems!["D"].Text);
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
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<DatabaseSettings>(xml);

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
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<ProgramSettings>(xml);

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
            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<DbSchemaSettings>(xml);

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
            var json = JsonCodec.Serialize(original);
            var restored = JsonCodec.Deserialize<SystemSettings>(json);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal("4.0.1", restored!.CommonConfiguration.Version);
            Assert.Equal("common", restored.BackendConfiguration.DatabaseId);
        }
    }
}
