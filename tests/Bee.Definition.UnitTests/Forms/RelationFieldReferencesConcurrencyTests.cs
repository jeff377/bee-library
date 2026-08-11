using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// 驗證 <see cref="FormTable.RelationFieldReferences"/> 的 lazy 建立在並行下只發生一次。
    /// </summary>
    /// <remarks>
    /// <c>FormSchema</c> 來自 process-wide 快取，因此兩個請求首次觸碰同一份 schema 時會在此相遇。
    /// 先前是無保護的 null 檢查：會重複建立、把不同實例交給不同呼叫端，而建立過程本身會擲例外
    /// （欄位對應有誤時），於是例外從一個看起來只是讀取的 property getter 冒出來、時機還不確定。
    /// </remarks>
    public class RelationFieldReferencesConcurrencyTests
    {
        private static FormTable BuildTable()
        {
            var table = new FormTable("Order", "訂單");
            table.Fields!.Add(new FormField { FieldName = "cust_id", DbType = FieldDbType.String });
            table.Fields.Add(new FormField { FieldName = "cust_name", DbType = FieldDbType.String });

            var relation = new FormField
            {
                FieldName = "cust_ref",
                DbType = FieldDbType.String,
                Type = FieldType.DbField,
                RelationProgId = "Customer",
            };
            relation.RelationFieldMappings!.Add(new FieldMapping("sys_name", "cust_name"));
            table.Fields.Add(relation);
            return table;
        }

        [Fact]
        [DisplayName("並行首次讀取 RelationFieldReferences 應只建立一份，且每個呼叫端拿到同一個實例")]
        public void RelationFieldReferences_ConcurrentFirstAccess_YieldsOneInstance()
        {
            var table = BuildTable();

            var results = new RelationFieldReferenceCollection[32];
            Parallel.For(0, results.Length, i => results[i] = table.RelationFieldReferences);

            // 不是「都非 null」而是「都是同一個」—— 重複建立正是先前的缺陷。
            Assert.All(results, r => Assert.Same(results[0], r));
        }

        [Fact]
        [DisplayName("RelationFieldReferences 應正確建出反向索引")]
        public void RelationFieldReferences_BuildsReverseIndex()
        {
            var references = BuildTable().RelationFieldReferences;

            Assert.Single(references);
            Assert.Equal("cust_name", references[0].FieldName);
            Assert.Equal("Customer", references[0].SourceProgId);
        }
    }
}
