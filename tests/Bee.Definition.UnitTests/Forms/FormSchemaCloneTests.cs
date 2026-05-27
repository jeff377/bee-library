using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// <see cref="FormSchema.Clone"/> 系列測試：scalar / 子集合 / 巢狀
    /// FieldMappings、ListItems / mutation 隔離 / 不污染來源 cache 實例。
    /// </summary>
    public class FormSchemaCloneTests
    {
        [Fact]
        [DisplayName("Clone 應產生獨立 FormSchema 實例（reference 不等）")]
        public void Clone_ReturnsNewInstance()
        {
            var source = BuildSchema();

            var clone = source.Clone();

            Assert.NotSame(source, clone);
            Assert.NotSame(source.Tables, clone.Tables);
            Assert.NotSame(source.Tables![0], clone.Tables![0]);
            Assert.NotSame(source.Tables![0].Fields, clone.Tables![0].Fields);
        }

        [Fact]
        [DisplayName("Clone 應 deep-copy scalar properties（ProgId / DisplayName / CategoryId / ListFields）")]
        public void Clone_CopiesScalarProperties()
        {
            var source = BuildSchema();

            var clone = source.Clone();

            Assert.Equal("Customer", clone.ProgId);
            Assert.Equal("Customer (raw)", clone.DisplayName);
            Assert.Equal("common", clone.CategoryId);
            Assert.Equal("sys_id,sys_name", clone.ListFields);
        }

        [Fact]
        [DisplayName("Clone 應 deep-copy Tables / Fields，每個 entry 都是獨立實例")]
        public void Clone_CopiesTablesAndFields()
        {
            var source = BuildSchema();

            var clone = source.Clone();

            Assert.Equal(source.Tables!.Count, clone.Tables!.Count);
            Assert.Equal(source.Tables![0].Fields!.Count, clone.Tables![0].Fields!.Count);

            // 對應位置欄位內容相等但實例獨立
            var srcField = source.Tables![0].Fields!["sys_name"];
            var cloneField = clone.Tables![0].Fields!["sys_name"];
            Assert.NotSame(srcField, cloneField);
            Assert.Equal(srcField.Caption, cloneField.Caption);
            Assert.Equal(srcField.DbType, cloneField.DbType);
        }

        [Fact]
        [DisplayName("Clone 後 mutate clone 不應污染來源 schema")]
        public void Clone_MutatingCloneDoesNotAffectSource()
        {
            var source = BuildSchema();

            var clone = source.Clone();
            clone.DisplayName = "Customer (localized)";
            clone.Tables![0].DisplayName = "Localized table";
            clone.Tables![0].Fields!["sys_name"].Caption = "客戶名稱";

            Assert.Equal("Customer (raw)", source.DisplayName);
            Assert.Equal("Customer (raw table)", source.Tables![0].DisplayName);
            Assert.Equal("Customer Name (raw)", source.Tables![0].Fields!["sys_name"].Caption);
        }

        [Fact]
        [DisplayName("Clone 應 deep-copy FormField.RelationFieldMappings")]
        public void Clone_CopiesRelationFieldMappings()
        {
            var source = new FormSchema("Order", "Order") { CategoryId = "sales" };
            var table = source.Tables!.Add("Order", "Order");
            var customerField = new FormField("customer_rowid", "Customer", FieldDbType.String)
            {
                RelationProgId = "Customer",
            };
            customerField.RelationFieldMappings!.Add("sys_id", "ref_customer_id");
            customerField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");
            table.Fields!.Add(customerField);

            var clone = source.Clone();
            var cloneField = clone.Tables![0].Fields!["customer_rowid"];

            Assert.Equal(2, cloneField.RelationFieldMappings!.Count);
            Assert.NotSame(customerField.RelationFieldMappings, cloneField.RelationFieldMappings);
            Assert.NotSame(customerField.RelationFieldMappings![0], cloneField.RelationFieldMappings![0]);
            Assert.Equal("sys_id", cloneField.RelationFieldMappings![0].SourceField);
            Assert.Equal("ref_customer_id", cloneField.RelationFieldMappings![0].DestinationField);

            // mutate clone 不影響來源
            cloneField.RelationFieldMappings![0].DestinationField = "ref_x";
            Assert.Equal("ref_customer_id", customerField.RelationFieldMappings![0].DestinationField);
        }

        [Fact]
        [DisplayName("Clone 應 deep-copy FormField.ListItems")]
        public void Clone_CopiesListItems()
        {
            var source = new FormSchema("Customer", "Customer") { CategoryId = "common" };
            var table = source.Tables!.Add("Customer", "Customer");
            var genderField = new FormField("gender", "Gender", FieldDbType.String);
            genderField.ListItems!.Add("M", "Male");
            genderField.ListItems!.Add("F", "Female");
            table.Fields!.Add(genderField);

            var clone = source.Clone();
            var cloneField = clone.Tables![0].Fields!["gender"];

            Assert.Equal(2, cloneField.ListItems!.Count);
            Assert.Equal("Male", cloneField.ListItems!["M"].Text);
            Assert.NotSame(genderField.ListItems, cloneField.ListItems);

            cloneField.ListItems!["M"].Text = "男";
            Assert.Equal("Male", genderField.ListItems!["M"].Text);
        }

        [Fact]
        [DisplayName("Clone 後來源不應有 SerializeState mutation（不像 XmlCodec.Serialize）")]
        public void Clone_DoesNotMutateSerializeState()
        {
            var source = BuildSchema();
            Assert.Equal(Bee.Base.Serialization.SerializeState.None, source.SerializeState);

            _ = source.Clone();

            // Clone 是純讀取，不應動到來源的 SerializeState（XmlCodec.Serialize 會！）
            Assert.Equal(Bee.Base.Serialization.SerializeState.None, source.SerializeState);
        }

        [Fact]
        [DisplayName("並行多 thread Clone 同一來源不互相干擾、各得獨立副本")]
        public void Clone_ParallelInvocations_ProduceIndependentCopies()
        {
            var source = BuildSchema();
            const int iterations = 50;
            var clones = new FormSchema[iterations];

            Parallel.For(0, iterations, i =>
            {
                var copy = source.Clone();
                // mutate this clone with a unique marker so we can later detect cross-contamination
                copy.DisplayName = $"clone-{i}";
                copy.Tables![0].Fields!["sys_name"].Caption = $"name-{i}";
                clones[i] = copy;
            });

            for (int i = 0; i < iterations; i++)
            {
                Assert.Equal($"clone-{i}", clones[i].DisplayName);
                Assert.Equal($"name-{i}", clones[i].Tables![0].Fields!["sys_name"].Caption);
            }
            // 源始 schema 應保持原狀
            Assert.Equal("Customer (raw)", source.DisplayName);
            Assert.Equal("Customer Name (raw)", source.Tables![0].Fields!["sys_name"].Caption);
        }

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Customer", "Customer (raw)")
            {
                CategoryId = "common",
                ListFields = "sys_id,sys_name",
            };
            var table = schema.Tables!.Add("Customer", "Customer (raw table)");
            table.DbTableName = "ft_customer";
            table.Fields!.Add("sys_id", "Customer ID (raw)", FieldDbType.String);
            table.Fields!.Add("sys_name", "Customer Name (raw)", FieldDbType.String);
            return schema;
        }
    }
}
