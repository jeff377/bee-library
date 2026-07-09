using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Expressions;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// <see cref="FormExpressionCalculator"/> 測試：row-level 計算（含 RoundByKind 捨入、宣告順序鏈）、
    /// 只填空欄的預設值、只回報實際變動欄、以及「來源欄 → 計算欄」相依圖。使用真實
    /// <see cref="DynamicExpressoEvaluator"/>，純邏輯、無資料庫。
    /// </summary>
    public class FormExpressionCalculatorTests
    {
        private readonly FormExpressionCalculator _calculator = new(new DynamicExpressoEvaluator());

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
            table.Fields!.Add(new FormField("tax", "Tax", FieldDbType.Currency)
            {
                NumberKind = NumberKind.Amount,
                ValueExpression = "amount * 0.05m",
                ReadOnly = true,
            });
            table.Fields!.Add(new FormField("order_date", "OrderDate", FieldDbType.DateTime)
            {
                DefaultValueExpression = "Today()",
            });
            table.Fields!.Add(new FormField("status", "Status", FieldDbType.String));
            return schema;
        }

        private static DataTable BuildOrderTable(decimal price, decimal qty,
            object? amount = null, object? tax = null, object? orderDate = null)
        {
            var table = new DataTable("Order");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("price", typeof(decimal));
            table.Columns.Add("qty", typeof(decimal));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("tax", typeof(decimal));
            table.Columns.Add("order_date", typeof(DateTime));
            table.Columns.Add("status", typeof(string));

            var row = table.NewRow();
            row["price"] = price;
            row["qty"] = qty;
            if (amount != null) { row["amount"] = amount; }
            if (tax != null) { row["tax"] = tax; }
            if (orderDate != null) { row["order_date"] = orderDate; }
            row["status"] = "Draft";
            table.Rows.Add(row);
            return table;
        }

        [Fact]
        [DisplayName("ApplyComputedRow：amount = price * qty 回填並回報變動欄")]
        public void ApplyComputedRow_ComputesAndReportsChanged()
        {
            var schema = BuildOrderSchema();
            var table = BuildOrderTable(price: 10m, qty: 3m);

            var changed = _calculator.ApplyComputedRow(schema, schema.MasterTable!, table.Rows[0], new RoundingContext());

            Assert.Equal(30m, table.Rows[0]["amount"]);
            Assert.Contains("amount", changed);
        }

        [Fact]
        [DisplayName("ApplyComputedRow：宣告順序鏈 tax 依 amount（同一次求值取得更新後的 amount）")]
        public void ApplyComputedRow_ChainsInDeclarationOrder()
        {
            var schema = BuildOrderSchema();
            var table = BuildOrderTable(price: 100m, qty: 2m);

            _calculator.ApplyComputedRow(schema, schema.MasterTable!, table.Rows[0], new RoundingContext());

            // amount = 200；tax = 200 * 0.05 = 10（tax 觀察到同一次求出的 amount，而非舊值）
            Assert.Equal(200m, table.Rows[0]["amount"]);
            Assert.Equal(10m, table.Rows[0]["tax"]);
        }

        [Fact]
        [DisplayName("ApplyComputedRow：Amount kind 依 NumberKind 捨入至 2 位（away-from-zero）")]
        public void ApplyComputedRow_RoundsByNumberKind()
        {
            var schema = BuildOrderSchema();
            // 2.125 * 1 = 2.125 → Amount 2 位、四捨五入(away) → 2.13
            var table = BuildOrderTable(price: 2.125m, qty: 1m);

            _calculator.ApplyComputedRow(schema, schema.MasterTable!, table.Rows[0], new RoundingContext());

            Assert.Equal(2.13m, table.Rows[0]["amount"]);
        }

        [Fact]
        [DisplayName("ApplyComputedRow：值未變動時不回報（compare-first，避免事件雜訊）")]
        public void ApplyComputedRow_NoChange_ReturnsEmpty()
        {
            var schema = BuildOrderSchema();
            // 先填好等於計算結果的 amount / tax，再算一次應無變動
            var table = BuildOrderTable(price: 10m, qty: 3m, amount: 30m, tax: 1.5m);

            var changed = _calculator.ApplyComputedRow(schema, schema.MasterTable!, table.Rows[0], new RoundingContext());

            Assert.Empty(changed);
        }

        [Fact]
        [DisplayName("ApplyDefaultRow：空欄以運算式填入並回報；已有值不覆寫")]
        public void ApplyDefaultRow_FillsOnlyEmpty()
        {
            var schema = BuildOrderSchema();
            var table = BuildOrderTable(price: 1m, qty: 1m);

            var changed = _calculator.ApplyDefaultRow(schema.MasterTable!, table.Rows[0]);

            Assert.Equal(DateTime.Today, table.Rows[0]["order_date"]);
            Assert.Contains("order_date", changed);

            // 第二次呼叫：order_date 已有值 → 不覆寫、不回報
            var again = _calculator.ApplyDefaultRow(schema.MasterTable!, table.Rows[0]);
            Assert.Empty(again);
        }

        [Fact]
        [DisplayName("大寫欄名（AddColumn 存大寫）下運算式仍解析：變數以宣告欄名為 key，非 DataColumn 名")]
        public void ApplyComputedRow_UppercaseColumnNames_StillResolvesIdentifiers()
        {
            var schema = BuildOrderSchema();
            // The framework's AddColumn stores column names uppercased; expressions use the declared
            // (lower-case) field names. DynamicExpresso identifiers are case-sensitive, so this shape
            // (the real wire/DataSet shape) must still resolve.
            var table = new DataTable("Order");
            table.Columns.Add("PRICE", typeof(decimal));
            table.Columns.Add("QTY", typeof(decimal));
            table.Columns.Add("AMOUNT", typeof(decimal));
            table.Columns.Add("TAX", typeof(decimal));
            table.Columns.Add("STATUS", typeof(string));
            var row = table.NewRow();
            row["PRICE"] = 10m;
            row["QTY"] = 3m;
            row["STATUS"] = "Draft";
            table.Rows.Add(row);

            var ex = Record.Exception(() =>
                _calculator.ApplyComputedRow(schema, schema.MasterTable!, table.Rows[0], new RoundingContext()));

            Assert.Null(ex);
            Assert.Equal(30m, table.Rows[0]["amount"]);   // DataRow lookup is case-insensitive
            Assert.Equal(1.5m, table.Rows[0]["tax"]);
        }

        [Fact]
        [DisplayName("大寫欄名下 ValidateRules 仍解析規則識別字（customer_rowid != Guid.Empty 型）")]
        public void ValidateRules_UppercaseColumnNames_StillResolvesIdentifiers()
        {
            var schema = BuildOrderSchema();
            schema.Rules!.Add("amount_positive", "amount > 0", "金額必須大於 0");
            var table = new DataTable("Order");
            table.Columns.Add("PRICE", typeof(decimal));
            table.Columns.Add("QTY", typeof(decimal));
            table.Columns.Add("AMOUNT", typeof(decimal));
            table.Columns.Add("TAX", typeof(decimal));
            table.Columns.Add("STATUS", typeof(string));
            table.Rows.Add(10m, 2m, 20m, 1m, "Draft");
            var dataSet = new DataSet("Order");
            dataSet.Tables.Add(table);

            var ex = Record.Exception(() =>
                _calculator.ValidateRules(schema, dataSet, FormRuleTrigger.BeforeSave));

            Assert.Null(ex);   // amount=20 > 0 → passes; must not throw UnknownIdentifier
        }

        [Fact]
        [DisplayName("BuildDependencyMap：price / qty → amount；amount → tax（來源欄對映受影響計算欄）")]
        public void BuildDependencyMap_MapsSourceToComputed()
        {
            var schema = BuildOrderSchema();

            var map = _calculator.BuildDependencyMap(schema.MasterTable!);

            Assert.Contains("amount", map["price"]);
            Assert.Contains("amount", map["qty"]);
            Assert.Contains("tax", map["amount"]);
            // status 不是任何計算欄的來源
            Assert.False(map.ContainsKey("status"));
        }

        [Fact]
        [DisplayName("BuildDependencyMap：來源欄 key 大小寫不敏感（對齊 DataTable 欄名查找）")]
        public void BuildDependencyMap_KeysAreCaseInsensitive()
        {
            var schema = BuildOrderSchema();

            var map = _calculator.BuildDependencyMap(schema.MasterTable!);

            Assert.True(map.ContainsKey("PRICE"));
            Assert.True(map.ContainsKey("Qty"));
        }
    }
}
