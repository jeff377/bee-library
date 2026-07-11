using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Db.UnitTests.Dml
{
    /// <summary>
    /// Coverage-focused tests for <see cref="SelectContextBuilder"/>. All DB-free: a stub
    /// <see cref="IDefineAccess"/> supplies the relation form schemas, so no database or
    /// fixture is required and these run during every coverage collection.
    /// </summary>
    public class SelectContextBuilderCoverageTests
    {
        // ---- helpers -------------------------------------------------------

        private static FormTable NewMainTable()
        {
            var table = new FormTable("Order", "Order") { DbTableName = "ft_order" };
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            return table;
        }

        private static FormField AddForeignKey(FormTable table, string fieldName, string relationProgId)
        {
            var field = table.Fields!.Add(fieldName, fieldName, FieldDbType.Guid);
            field.RelationProgId = relationProgId;
            return field;
        }

        private static FormSchema NewRelationSchema(string progId, params string[] sourceFields)
        {
            var schema = new FormSchema(progId, progId);
            var table = schema.Tables!.Add(progId, progId);
            table.DbTableName = "ft_" + progId.ToLowerInvariant();
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            foreach (var name in sourceFields)
                table.Fields!.AddStringField(name, name, 50);
            return schema;
        }

        // ---- tests ---------------------------------------------------------

        [Fact]
        [DisplayName("建構子 defineAccess 為 null 應擲 ArgumentNullException")]
        public void Constructor_NullDefineAccess_Throws()
        {
            var table = NewMainTable();
            Assert.Throws<ArgumentNullException>(() =>
                new SelectContextBuilder(table, [], null!));
        }

        [Fact]
        [DisplayName("Build 外鍵欄位可解析時應建立 JOIN 與 QueryFieldMapping")]
        public void Build_ResolvableForeignKey_ProducesJoinAndMapping()
        {
            var table = NewMainTable();
            var fk = AddForeignKey(table, "category_rowid", "Category");
            fk.RelationFieldMappings!.Add("sys_id", "ref_category_id");
            fk.RelationFieldMappings!.Add("sys_name", "ref_category_name");

            var used = new HashSet<string> { "ref_category_id" };
            var define = new StubDefineAccess(_ => NewRelationSchema("Category", "sys_id", "sys_name"));
            var context = new SelectContextBuilder(table, used, define).Build();

            Assert.Single(context.Joins);
            Assert.Equal("B", context.Joins[0].RightAlias);
            Assert.Single(context.FieldMappings);
        }

        [Fact]
        [DisplayName("Build 關係 FormSchema 不存在應擲 InvalidOperationException")]
        public void Build_RelationSchemaNotFound_Throws()
        {
            var table = NewMainTable();
            var fk = AddForeignKey(table, "category_rowid", "Missing");
            fk.RelationFieldMappings!.Add("sys_id", "ref_category_id");

            var used = new HashSet<string> { "ref_category_id" };
            var define = new StubDefineAccess(_ => null);
            var builder = new SelectContextBuilder(table, used, define);

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        [DisplayName("Build 來源欄位不存在於關係資料表應擲 InvalidOperationException")]
        public void Build_SourceFieldNotFound_Throws()
        {
            var table = NewMainTable();
            var fk = AddForeignKey(table, "category_rowid", "Category");
            fk.RelationFieldMappings!.Add("no_such_field", "ref_category_id");

            var used = new HashSet<string> { "ref_category_id" };
            // Relation schema does NOT contain "no_such_field".
            var define = new StubDefineAccess(_ => NewRelationSchema("Category", "sys_id"));
            var builder = new SelectContextBuilder(table, used, define);

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        [DisplayName("Build 外鍵欄位 RelationFieldMappings 為 null 時應略過該欄位")]
        public void Build_ForeignKeyWithNullMappings_SkipsField()
        {
            var table = NewMainTable();
            var fk = AddForeignKey(table, "category_rowid", "Category");
            // Under serialize state an untouched (empty) RelationFieldMappings getter returns
            // null, exercising the null-mappings early-return in GetUsedRelationFieldMappings.
            fk.SetSerializeState(SerializeState.Serialize);

            var used = new HashSet<string> { "ref_category_id" };
            var define = new StubDefineAccess(_ => NewRelationSchema("Category", "sys_id"));
            var context = new SelectContextBuilder(table, used, define).Build();

            Assert.Empty(context.Joins);
        }

        [Fact]
        [DisplayName("Build 產生足夠多的 JOIN 時應跳過 SQL 保留字別名（BY）")]
        public void Build_ManyJoins_SkipsReservedKeywordAlias()
        {
            // Alias progression is A, B..Z, BA..BZ. The 50th generated alias would be "BY",
            // a reserved SQL keyword, which the builder must skip (advancing to "BZ").
            const int fieldCount = 50;
            var table = NewMainTable();
            for (int i = 0; i < fieldCount; i++)
            {
                var fk = AddForeignKey(table, "fk" + i, "Ref");
                fk.RelationFieldMappings!.Add("sys_id", "d" + i);
            }

            var used = new HashSet<string>();
            for (int i = 0; i < fieldCount; i++)
                used.Add("d" + i);

            var define = new StubDefineAccess(_ => NewRelationSchema("Ref", "sys_id"));
            var context = new SelectContextBuilder(table, used, define).Build();

            Assert.Equal(fieldCount, context.Joins.Count);
            Assert.DoesNotContain(context.Joins, j => j.RightAlias == "BY");
            Assert.Contains(context.Joins, j => j.RightAlias == "BZ");
        }

        // ---- stub ----------------------------------------------------------

        /// <summary>
        /// Minimal <see cref="IDefineAccess"/> that only serves form schemas via a delegate;
        /// every other member is unused by <see cref="SelectContextBuilder"/> and throws.
        /// </summary>
        private sealed class StubDefineAccess : IDefineAccess
        {
            private readonly Func<string, FormSchema?> _getFormSchema;
            public StubDefineAccess(Func<string, FormSchema?> getFormSchema) => _getFormSchema = getFormSchema;

            public FormSchema GetFormSchema(string progId) => _getFormSchema(progId)!;

            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotSupportedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotSupportedException();
            public SystemSettings GetSystemSettings() => throw new NotSupportedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotSupportedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotSupportedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotSupportedException();
            public ProgramSettings GetProgramSettings() => throw new NotSupportedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotSupportedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotSupportedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotSupportedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotSupportedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotSupportedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotSupportedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotSupportedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotSupportedException();
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotSupportedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotSupportedException();
        }
    }
}
