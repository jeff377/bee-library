using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableSchemaDiffTests
    {
        private static TableSchema BuildSchema(string tableName = "st_demo")
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add("id", "Id", FieldDbType.Guid);
            return schema;
        }

        [Fact]
        [DisplayName("新建 Diff 時 Changes 與 DescriptionChanges 皆為空")]
        public void Constructor_InitializesEmptyCollections()
        {
            var define = BuildSchema();
            var diff = new TableSchemaDiff(define, realTable: null);

            Assert.Empty(diff.Changes);
            Assert.Empty(diff.DescriptionChanges);
        }

        [Fact]
        [DisplayName("DefineTable 與 RealTable 應正確暴露")]
        public void Properties_ExposeInputs()
        {
            var define = BuildSchema();
            var real = BuildSchema();
            var diff = new TableSchemaDiff(define, real);

            Assert.Same(define, diff.DefineTable);
            Assert.Same(real, diff.RealTable);
        }

        [Fact]
        [DisplayName("RealTable 為 null 時 IsNewTable 應為 true")]
        public void IsNewTable_NullRealTable_ReturnsTrue()
        {
            var diff = new TableSchemaDiff(BuildSchema(), realTable: null);

            Assert.True(diff.IsNewTable);
        }

        [Fact]
        [DisplayName("RealTable 非 null 時 IsNewTable 應為 false")]
        public void IsNewTable_NonNullRealTable_ReturnsFalse()
        {
            var diff = new TableSchemaDiff(BuildSchema(), BuildSchema());

            Assert.False(diff.IsNewTable);
        }

        [Fact]
        [DisplayName("IsNewTable 為 true 時 IsEmpty 應為 false")]
        public void IsEmpty_NewTable_ReturnsFalse()
        {
            var diff = new TableSchemaDiff(BuildSchema(), realTable: null);

            Assert.False(diff.IsEmpty);
        }

        [Fact]
        [DisplayName("無任何變化時 IsEmpty 應為 true")]
        public void IsEmpty_NoChanges_ReturnsTrue()
        {
            var diff = new TableSchemaDiff(BuildSchema(), BuildSchema());

            Assert.True(diff.IsEmpty);
        }

        [Fact]
        [DisplayName("含 Structural Change 時 IsEmpty 應為 false")]
        public void IsEmpty_WithStructuralChange_ReturnsFalse()
        {
            var diff = new TableSchemaDiff(BuildSchema(), BuildSchema());
            diff.Changes.Add(new AddFieldChange(new DbField("age", "Age", FieldDbType.Integer)));

            Assert.False(diff.IsEmpty);
        }

        [Fact]
        [DisplayName("含 Description Change 時 IsEmpty 應為 false")]
        public void IsEmpty_WithDescriptionChange_ReturnsFalse()
        {
            var diff = new TableSchemaDiff(BuildSchema(), BuildSchema());
            diff.DescriptionChanges.Add(new DescriptionChange
            {
                Level = DescriptionLevel.Table,
                NewValue = "示範",
                IsNew = true,
            });

            Assert.False(diff.IsEmpty);
        }
    }
}
