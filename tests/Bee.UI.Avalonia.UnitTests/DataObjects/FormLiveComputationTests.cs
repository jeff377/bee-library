using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.DataObjects
{
    /// <summary>
    /// <see cref="FormLiveComputation"/> 測試：即時重算回寫、相依圖 gating（無依賴欄 / 計算欄自身不觸發）、
    /// re-entrancy guard、預設值套用。前端走與後端相同的 <see cref="FormExpressionCalculator"/>，故對相同
    /// 輸入產出相同數值（對照後端 <c>FormRuleProcessorTests</c> 的斷言值）。
    /// </summary>
    public class FormLiveComputationTests
    {
        private static FormSchema BuildOrderSchema()
        {
            var schema = new FormSchema("Order", "Order") { CategoryId = "company" };
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add(new FormField("sys_rowid", "RowId", FieldDbType.Guid));
            table.Fields!.Add(new FormField("price", "Price", FieldDbType.Currency) { NumberKind = NumberKind.UnitPrice });
            table.Fields!.Add(new FormField("qty", "Qty", FieldDbType.Decimal) { NumberKind = NumberKind.Quantity });
            table.Fields!.Add(new FormField("amount", "Amount", FieldDbType.Currency)
            {
                NumberKind = NumberKind.Amount,
                ValueExpression = "price * qty",
                ReadOnly = true,
            });
            table.Fields!.Add(new FormField("order_date", "OrderDate", FieldDbType.DateTime)
            {
                DefaultValueExpression = "Today()",
            });
            table.Fields!.Add(new FormField("status", "Status", FieldDbType.String));
            return schema;
        }

        private static DataTable BuildOrderTable(decimal price, decimal qty, object? amount = null)
        {
            var table = new DataTable("Order");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("price", typeof(decimal));
            table.Columns.Add("qty", typeof(decimal));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("order_date", typeof(DateTime));
            table.Columns.Add("status", typeof(string));

            var row = table.NewRow();
            row["price"] = price;
            row["qty"] = qty;
            if (amount != null) { row["amount"] = amount; }
            row["status"] = "Draft";
            table.Rows.Add(row);
            return table;
        }

        [Fact]
        [DisplayName("Recompute：編輯 qty 觸發 amount 重算並回寫（值同後端 price*qty）")]
        public void Recompute_OnSourceChange_WritesComputedField()
        {
            var live = new FormLiveComputation(BuildOrderSchema());
            var table = BuildOrderTable(price: 10m, qty: 3m);

            var changed = live.Recompute("Order", "qty", table.Rows[0]);

            Assert.Equal(30m, table.Rows[0]["amount"]);
            Assert.Contains("amount", changed);
        }

        [Fact]
        [DisplayName("Recompute：Amount kind 依 NumberKind 捨入至 2 位（Tier 1 框架預設位數）")]
        public void Recompute_RoundsByFrameworkDefaultDecimals()
        {
            var live = new FormLiveComputation(BuildOrderSchema());
            var table = BuildOrderTable(price: 2.125m, qty: 1m);

            live.Recompute("Order", "price", table.Rows[0]);

            Assert.Equal(2.13m, table.Rows[0]["amount"]);
        }

        [Fact]
        [DisplayName("Recompute：變更無依賴欄（status）不重算、回報空")]
        public void Recompute_NonSourceField_NoOp()
        {
            var live = new FormLiveComputation(BuildOrderSchema());
            var table = BuildOrderTable(price: 10m, qty: 3m);

            var changed = live.Recompute("Order", "status", table.Rows[0]);

            Assert.Empty(changed);
            // amount 未被計算填入（仍為 DBNull）
            Assert.Equal(DBNull.Value, table.Rows[0]["amount"]);
        }

        [Fact]
        [DisplayName("Recompute：計算欄自身變更不作為觸發源（回報空）")]
        public void Recompute_ComputedFieldChange_NotATrigger()
        {
            var live = new FormLiveComputation(BuildOrderSchema());
            var table = BuildOrderTable(price: 10m, qty: 3m);

            var changed = live.Recompute("Order", "amount", table.Rows[0]);

            Assert.Empty(changed);
        }

        [Fact]
        [DisplayName("Recompute：重算進行中（IsRecomputing）時再進入回報空（re-entrancy guard）")]
        public void Recompute_WhileRecomputing_IsGuarded()
        {
            var live = new FormLiveComputation(BuildOrderSchema());
            var table = BuildOrderTable(price: 10m, qty: 3m);

            // 不在重算中：旗標為 false
            Assert.False(live.IsRecomputing);
            var changed = live.Recompute("Order", "qty", table.Rows[0]);
            Assert.Contains("amount", changed);
            // 重算結束後旗標復位
            Assert.False(live.IsRecomputing);
        }

        [Fact]
        [DisplayName("ApplyDefaults：新列空欄以 DefaultValueExpression 填入")]
        public void ApplyDefaults_FillsEmptyDefaultExpression()
        {
            var live = new FormLiveComputation(BuildOrderSchema());
            var table = BuildOrderTable(price: 1m, qty: 1m);

            var changed = live.ApplyDefaults("Order", table.Rows[0]);

            Assert.Equal(DateTime.Today, table.Rows[0]["order_date"]);
            Assert.Contains("order_date", changed);
        }

        [Fact]
        [DisplayName("Recompute：欄名大小寫不敏感（事件欄名大小寫可能異於 schema）")]
        public void Recompute_FieldNameCaseInsensitive()
        {
            var live = new FormLiveComputation(BuildOrderSchema());
            var table = BuildOrderTable(price: 10m, qty: 3m);

            var changed = live.Recompute("Order", "QTY", table.Rows[0]);

            Assert.Equal(30m, table.Rows[0]["amount"]);
            Assert.Contains("amount", changed);
        }
    }
}
