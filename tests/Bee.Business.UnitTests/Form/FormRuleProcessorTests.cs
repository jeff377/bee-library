using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Base.Exceptions;
using Bee.Business.Form;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Expressions;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// <see cref="FormRuleProcessor"/> 測試：計算欄（含 RoundByKind 捨入）、預設值運算式、
    /// BeforeSave 驗證（含 When 適用性）、BeforeDelete 驗證。使用真實 DynamicExpressoEvaluator，
    /// 純邏輯、無資料庫。
    /// </summary>
    public class FormRuleProcessorTests
    {
        private readonly FormRuleProcessor _processor = new(new DynamicExpressoEvaluator());

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

        private static DataSet BuildOrderDataSet(decimal price, decimal qty, string status,
            object? orderDate = null, object? amount = null)
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
            row["status"] = status;
            if (amount != null) { row["amount"] = amount; }
            if (orderDate != null) { row["order_date"] = orderDate; }
            table.Rows.Add(row);   // RowState = Added

            var dataSet = new DataSet("Order");
            dataSet.Tables.Add(table);
            return dataSet;
        }

        [Fact]
        [DisplayName("計算欄：amount = price * qty，Added 列於存檔前回填")]
        public void ApplyBeforeSave_ComputesAmount_OnAddedRow()
        {
            var schema = BuildOrderSchema();
            var dataSet = BuildOrderDataSet(price: 10m, qty: 3m, status: "Draft");

            _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext());

            Assert.Equal(30m, dataSet.Tables["Order"]!.Rows[0]["amount"]);
        }

        [Fact]
        [DisplayName("計算欄：Amount kind 依 NumberKind 捨入至 2 位（away-from-zero）")]
        public void ApplyBeforeSave_RoundsAmountByNumberKind()
        {
            var schema = BuildOrderSchema();
            // 2.125 * 1 = 2.125 → Amount 2 位、四捨五入(away) → 2.13
            var dataSet = BuildOrderDataSet(price: 2.125m, qty: 1m, status: "Draft");

            _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext());

            Assert.Equal(2.13m, dataSet.Tables["Order"]!.Rows[0]["amount"]);
        }

        [Fact]
        [DisplayName("預設值運算式：Added 列空欄以 Today() 填入")]
        public void ApplyBeforeSave_FillsDefaultValueExpression_WhenEmpty()
        {
            var schema = BuildOrderSchema();
            var dataSet = BuildOrderDataSet(price: 1m, qty: 1m, status: "Draft");

            _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext());

            Assert.Equal(DateTime.Today, dataSet.Tables["Order"]!.Rows[0]["order_date"]);
        }

        [Fact]
        [DisplayName("預設值運算式：欄位已有值時不覆寫")]
        public void ApplyBeforeSave_DoesNotOverwriteExistingDefault()
        {
            var schema = BuildOrderSchema();
            var existing = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var dataSet = BuildOrderDataSet(price: 1m, qty: 1m, status: "Draft", orderDate: existing);

            _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext());

            Assert.Equal(existing, dataSet.Tables["Order"]!.Rows[0]["order_date"]);
        }

        [Fact]
        [DisplayName("存檔前驗證：Condition 不通過拋 UserMessageException 並帶訊息")]
        public void ApplyBeforeSave_FailingRule_ThrowsUserMessage()
        {
            var schema = BuildOrderSchema();
            schema.Rules!.Add("amount_positive", "amount > 0", "金額必須大於 0");
            var dataSet = BuildOrderDataSet(price: 0m, qty: 5m, status: "Draft");

            var ex = Assert.Throws<UserMessageException>(() =>
                _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext()));

            Assert.Equal("金額必須大於 0", ex.Message);
        }

        [Fact]
        [DisplayName("存檔前驗證：Condition 通過不拋例外")]
        public void ApplyBeforeSave_PassingRule_DoesNotThrow()
        {
            var schema = BuildOrderSchema();
            schema.Rules!.Add("amount_positive", "amount > 0", "金額必須大於 0");
            var dataSet = BuildOrderDataSet(price: 10m, qty: 2m, status: "Draft");

            var ex = Record.Exception(() =>
                _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext()));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("存檔前驗證：When 不成立時整條規則略過（不檢查 Condition）")]
        public void ApplyBeforeSave_WhenFalse_SkipsRule()
        {
            var schema = BuildOrderSchema();
            schema.Rules!.Add(new FormRule("approved_amount", "amount > 0", "已核准金額必須大於 0")
            {
                When = "status == \"Approved\"",
            });
            // status=Draft、amount=0：When 不成立 → 略過 → 不拋
            var dataSet = BuildOrderDataSet(price: 0m, qty: 1m, status: "Draft");

            var ex = Record.Exception(() =>
                _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext()));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("存檔前驗證：When 成立且 Condition 不通過時中斷")]
        public void ApplyBeforeSave_WhenTrueAndConditionFails_Throws()
        {
            var schema = BuildOrderSchema();
            schema.Rules!.Add(new FormRule("approved_amount", "amount > 0", "已核准金額必須大於 0")
            {
                When = "status == \"Approved\"",
            });
            var dataSet = BuildOrderDataSet(price: 0m, qty: 1m, status: "Approved");

            var ex = Assert.Throws<UserMessageException>(() =>
                _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext()));

            Assert.Equal("已核准金額必須大於 0", ex.Message);
        }

        [Fact]
        [DisplayName("刪除前檢查：BeforeDelete 規則不通過拋 UserMessageException")]
        public void ApplyBeforeDelete_FailingRule_Throws()
        {
            var schema = BuildOrderSchema();
            schema.Rules!.Add(new FormRule("no_delete_closed", "status != \"Closed\"", "已結案不可刪除")
            {
                Trigger = FormRuleTrigger.BeforeDelete,
            });
            var snapshot = BuildOrderDataSet(price: 1m, qty: 1m, status: "Closed");
            snapshot.AcceptChanges();   // 快照列為 Unchanged

            var ex = Assert.Throws<UserMessageException>(() =>
                _processor.ApplyBeforeDelete(schema, snapshot));

            Assert.Equal("已結案不可刪除", ex.Message);
        }

        [Fact]
        [DisplayName("刪除前檢查：BeforeDelete 規則於存檔前不觸發（trigger 隔離）")]
        public void ApplyBeforeSave_DoesNotRunBeforeDeleteRules()
        {
            var schema = BuildOrderSchema();
            schema.Rules!.Add(new FormRule("no_delete_closed", "status != \"Closed\"", "已結案不可刪除")
            {
                Trigger = FormRuleTrigger.BeforeDelete,
            });
            var dataSet = BuildOrderDataSet(price: 1m, qty: 1m, status: "Closed");

            var ex = Record.Exception(() =>
                _processor.ApplyBeforeSave(schema, dataSet, new RoundingContext()));

            Assert.Null(ex);
        }
    }
}
