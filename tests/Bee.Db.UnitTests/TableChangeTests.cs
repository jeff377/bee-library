using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableChangeTests
    {
        [Fact]
        [DisplayName("AddFieldChange 應保留 Field 參考且為 TableChange 子類")]
        public void AddFieldChange_PreservesField()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer);
            var change = new AddFieldChange(field);

            Assert.Same(field, change.Field);
            Assert.IsAssignableFrom<TableChange>(change);
        }

        [Fact]
        [DisplayName("AlterFieldChange 應保留 OldField 與 NewField 參考")]
        public void AlterFieldChange_PreservesOldAndNewFields()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 30 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var change = new AlterFieldChange(oldField, newField);

            Assert.Same(oldField, change.OldField);
            Assert.Same(newField, change.NewField);
            Assert.IsAssignableFrom<TableChange>(change);
        }

        [Fact]
        [DisplayName("AddIndexChange 應保留 Index 參考")]
        public void AddIndexChange_PreservesIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");
            var change = new AddIndexChange(index);

            Assert.Same(index, change.Index);
            Assert.IsAssignableFrom<TableChange>(change);
        }

        [Fact]
        [DisplayName("DropIndexChange 應保留 Index 參考")]
        public void DropIndexChange_PreservesIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");
            var change = new DropIndexChange(index);

            Assert.Same(index, change.Index);
            Assert.IsAssignableFrom<TableChange>(change);
        }

        [Fact]
        [DisplayName("RenameFieldChange 應保留 OldFieldName 與 NewField")]
        public void RenameFieldChange_PreservesOldNameAndNewField()
        {
            var newField = new DbField("employee_name", "Employee Name", FieldDbType.String) { Length = 50 };
            var change = new RenameFieldChange("emp_name", newField);

            Assert.Equal("emp_name", change.OldFieldName);
            Assert.Same(newField, change.NewField);
            Assert.IsAssignableFrom<TableChange>(change);
        }
    }
}
