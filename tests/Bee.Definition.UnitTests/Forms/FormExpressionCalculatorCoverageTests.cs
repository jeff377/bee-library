using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Base.Exceptions;
using Bee.Base.Serialization;
using Bee.Definition.Forms;
using Bee.Expressions;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// <see cref="FormExpressionCalculator"/> 補測：涵蓋整表 <c>ApplyFieldExpressions</c> 對不同 RowState
    /// 的分支、<c>ValidateRules</c> 的規則過濾／When 守衛／違規丟出 <see cref="UserMessageException"/>／
    /// 目標表解析、參考碼（幣別 / 單位）捨入解析，以及 null Fields 的早退分支。使用真實
    /// <see cref="DynamicExpressoEvaluator"/>，純記憶體、無資料庫。
    /// </summary>
    public class FormExpressionCalculatorCoverageTests
    {
        private readonly FormExpressionCalculator _calculator = new(new DynamicExpressoEvaluator());

        private static FormSchema BuildComputeSchema()
        {
            var schema = new FormSchema("Order", "Order") { CategoryId = "company" };
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add(new FormField("sys_rowid", "RowId", FieldDbType.Guid));
            table.Fields!.Add(new FormField("price", "Price", FieldDbType.Currency));
            table.Fields!.Add(new FormField("qty", "Qty", FieldDbType.Decimal));
            table.Fields!.Add(new FormField("amount", "Amount", FieldDbType.Currency)
            {
                NumberKind = NumberKind.Amount,
                ValueExpression = "price * qty",
            });
            table.Fields!.Add(new FormField("status", "Status", FieldDbType.String)
            {
                DefaultValueExpression = "\"Draft\"",
            });
            return schema;
        }

        private static DataTable BuildComputeTable()
        {
            var table = new DataTable("Order");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("price", typeof(decimal));
            table.Columns.Add("qty", typeof(decimal));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("extra_col", typeof(string));   // 無對映 schema 欄 → BuildVariables fallback
            return table;
        }

        [Fact]
        [DisplayName("ApplyFieldExpressions：schema.Tables 為 null（序列化空集合）時早退、不丟例外")]
        public void ApplyFieldExpressions_NullTables_ReturnsEarly()
        {
            var schema = new FormSchema("Order", "Order") { CategoryId = "company" };
            schema.SetSerializeState(SerializeState.Serialize);   // 空 Tables → getter 回 null

            var ex = Record.Exception(() =>
                _calculator.ApplyFieldExpressions(schema, new DataSet(), new RoundingContext()));

            Assert.Null(ex);
            Assert.Null(schema.Tables);
        }

        [Fact]
        [DisplayName("ApplyFieldExpressions：資料集缺對應表時略過、不丟例外")]
        public void ApplyFieldExpressions_MissingDataTable_Skips()
        {
            var schema = BuildComputeSchema();

            var ex = Record.Exception(() =>
                _calculator.ApplyFieldExpressions(schema, new DataSet(), new RoundingContext()));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("ApplyFieldExpressions：僅對 Added / Modified 列重算；Unchanged 略過、Deleted 不觸碰")]
        public void ApplyFieldExpressions_RowStates_AppliesToAddedAndModifiedOnly()
        {
            var schema = BuildComputeSchema();
            var table = BuildComputeTable();

            var unchanged = table.Rows.Add(Guid.NewGuid(), 1m, 1m, 99m, "Keep", "x");
            var modified = table.Rows.Add(Guid.NewGuid(), 5m, 2m, 0m, "Keep", "y");
            var deleted = table.Rows.Add(Guid.NewGuid(), 9m, 9m, 7m, "Keep", "z");
            table.AcceptChanges();                       // 全部轉 Unchanged
            modified["price"] = 5m; modified["qty"] = 4m; // → Modified，amount 陳舊
            deleted.Delete();                            // → Deleted
            var added = table.Rows.Add(Guid.NewGuid(), 3m, 3m, 0m, DBNull.Value, "w"); // → Added

            var dataSet = new DataSet();
            dataSet.Tables.Add(table);

            _calculator.ApplyFieldExpressions(schema, dataSet, new RoundingContext());

            Assert.Equal(20m, modified["amount"]);   // 5 * 4 重算
            Assert.Equal(9m, added["amount"]);       // 3 * 3 重算
            Assert.Equal("Draft", added["status"]);  // Added 套用預設值
            Assert.Equal(99m, unchanged["amount"]);  // Unchanged 未被重算（分支 false）
        }

        [Fact]
        [DisplayName("ValidateRules：schema.Rules 為 null（序列化空集合）時早退、不丟例外")]
        public void ValidateRules_NullRules_ReturnsEarly()
        {
            var schema = BuildComputeSchema();
            schema.SetSerializeState(SerializeState.Serialize);   // 空 Rules → getter 回 null

            var ex = Record.Exception(() =>
                _calculator.ValidateRules(schema, new DataSet(), FormRuleTrigger.BeforeSave));

            Assert.Null(ex);
            Assert.Null(schema.Rules);
        }

        [Fact]
        [DisplayName("ValidateRules：只評估啟用且觸發點相符的規則、依 Order 排序，且略過 Deleted 列")]
        public void ValidateRules_FiltersEnabledMatchingTrigger_AndSkipsDeletedRow()
        {
            var schema = BuildComputeSchema();
            var r1 = schema.Rules!.Add("r_price", "price >= 0", "price");
            r1.Order = 2;
            var r1b = schema.Rules!.Add("r_qty", "qty >= 0", "qty");
            r1b.Order = 1;
            var disabled = schema.Rules!.Add("r_dis", "price > 1000000", "disabled");
            disabled.Enabled = false;                        // 停用 → 不評估
            var delTrigger = schema.Rules!.Add("r_del", "false", "wrong-trigger");
            delTrigger.Trigger = FormRuleTrigger.BeforeDelete; // 觸發點不符 → 不評估

            var table = BuildComputeTable();
            table.Rows.Add(Guid.NewGuid(), 10m, 2m, 20m, "Draft", "a");
            var deleted = table.Rows.Add(Guid.NewGuid(), 1m, 1m, 1m, "Draft", "b");
            table.AcceptChanges();
            deleted.Delete();                                // Deleted → ValidateRuleRows 略過
            var dataSet = new DataSet();
            dataSet.Tables.Add(table);

            var ex = Record.Exception(() =>
                _calculator.ValidateRules(schema, dataSet, FormRuleTrigger.BeforeSave));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("ValidateRules：條件不成立時以規則訊息丟出 UserMessageException")]
        public void ValidateRules_FailingCondition_ThrowsUserMessageException()
        {
            var schema = BuildComputeSchema();
            schema.Rules!.Add("r_fail", "amount > 100", "金額太小");
            var table = BuildComputeTable();
            table.Rows.Add(Guid.NewGuid(), 1m, 1m, 10m, "Draft", "a");
            var dataSet = new DataSet();
            dataSet.Tables.Add(table);

            var ex = Assert.Throws<UserMessageException>(() =>
                _calculator.ValidateRules(schema, dataSet, FormRuleTrigger.BeforeSave));

            Assert.Equal("金額太小", ex.Message);
        }

        [Fact]
        [DisplayName("ValidateRules：When 守衛不成立時整條規則略過（Condition 不評估）")]
        public void ValidateRules_WhenGuardFalse_SkipsRule()
        {
            var schema = BuildComputeSchema();
            var rule = schema.Rules!.Add("r_when", "false", "should-not-throw");
            rule.When = "status == \"Confirmed\"";   // 列 status = Draft → When false → 略過
            var table = BuildComputeTable();
            table.Rows.Add(Guid.NewGuid(), 1m, 1m, 10m, "Draft", "a");
            var dataSet = new DataSet();
            dataSet.Tables.Add(table);

            var ex = Record.Exception(() =>
                _calculator.ValidateRules(schema, dataSet, FormRuleTrigger.BeforeSave));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("ValidateRules：目標表解析——空 TargetTable→主檔、具名存在→該表、具名不存在→略過")]
        public void ValidateRules_TargetTableResolution_HandlesAllCases()
        {
            var schema = BuildComputeSchema();
            var detail = schema.Tables!.Add("OrderLine", "Lines");
            detail.Fields!.Add(new FormField("line_qty", "LineQty", FieldDbType.Integer));

            schema.Rules!.Add("r_master", "price >= 0", "master");           // 空 TargetTable → 主檔
            var rDetail = schema.Rules!.Add("r_detail", "line_qty >= 0", "detail");
            rDetail.TargetTable = "OrderLine";                               // 具名存在
            var rAbsent = schema.Rules!.Add("r_absent", "false", "absent");
            rAbsent.TargetTable = "NoSuchTable";                            // 具名不存在 → 略過

            var master = BuildComputeTable();
            master.Rows.Add(Guid.NewGuid(), 10m, 2m, 20m, "Draft", "a");
            var lineTable = new DataTable("OrderLine");
            lineTable.Columns.Add("line_qty", typeof(int));
            lineTable.Rows.Add(3);
            var dataSet = new DataSet();
            dataSet.Tables.Add(master);
            dataSet.Tables.Add(lineTable);

            var ex = Record.Exception(() =>
                _calculator.ValidateRules(schema, dataSet, FormRuleTrigger.BeforeSave));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("ApplyComputedRow：捨入參考碼解析——欄位幣別 / schema 幣別 / 遺漏幣別 / 單位 / 無參考碼")]
        public void ApplyComputedRow_ResolvesRefCodeAcrossNumberKinds()
        {
            var schema = new FormSchema("Order", "Order") { CategoryId = "company", CurrencyField = "currency" };
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add(new FormField("sys_rowid", "RowId", FieldDbType.Guid));
            table.Fields!.Add(new FormField("currency", "Currency", FieldDbType.String));
            table.Fields!.Add(new FormField("unit", "Unit", FieldDbType.String));
            table.Fields!.Add(new FormField("price", "Price", FieldDbType.Currency));
            table.Fields!.Add(new FormField("qty", "Qty", FieldDbType.Decimal));
            // Amount，欄位自帶 CurrencyField（變數含該欄）
            table.Fields!.Add(new FormField("amt_field_ccy", "AmtFieldCcy", FieldDbType.Currency)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "currency",
                ValueExpression = "price * qty",
            });
            // Amount，欄位無 CurrencyField → 退回 schema.CurrencyField
            table.Fields!.Add(new FormField("amt_schema_ccy", "AmtSchemaCcy", FieldDbType.Currency)
            {
                NumberKind = NumberKind.Amount,
                ValueExpression = "price * qty",
            });
            // Amount，CurrencyField 指向不存在的欄 → 變數查無 → refCode 為 null
            table.Fields!.Add(new FormField("amt_missing_ccy", "AmtMissingCcy", FieldDbType.Currency)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "ghost",
                ValueExpression = "price * qty",
            });
            // Weight，UnitField 提供單位參考碼
            table.Fields!.Add(new FormField("wt_field", "Weight", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Weight,
                UnitField = "unit",
                ValueExpression = "qty * 2",
            });
            // UnitPrice：非 Amount/Quantity/Weight → refCode 為 null（default 分支）
            table.Fields!.Add(new FormField("up_field", "UnitPrice", FieldDbType.Currency)
            {
                NumberKind = NumberKind.UnitPrice,
                ValueExpression = "price + 1",
            });

            var dataTable = new DataTable("Order");
            dataTable.Columns.Add("sys_rowid", typeof(Guid));
            dataTable.Columns.Add("currency", typeof(string));
            dataTable.Columns.Add("unit", typeof(string));
            dataTable.Columns.Add("price", typeof(decimal));
            dataTable.Columns.Add("qty", typeof(decimal));
            dataTable.Columns.Add("amt_field_ccy", typeof(decimal));
            dataTable.Columns.Add("amt_schema_ccy", typeof(decimal));
            dataTable.Columns.Add("amt_missing_ccy", typeof(decimal));
            dataTable.Columns.Add("wt_field", typeof(decimal));
            dataTable.Columns.Add("up_field", typeof(decimal));
            var row = dataTable.NewRow();
            row["sys_rowid"] = Guid.NewGuid();
            row["currency"] = "USD";
            row["unit"] = "KG";
            row["price"] = 10m;
            row["qty"] = 3m;
            dataTable.Rows.Add(row);

            var changed = _calculator.ApplyComputedRow(schema, schema.MasterTable!, row, new RoundingContext());

            Assert.Equal(30m, row["amt_field_ccy"]);
            Assert.Equal(30m, row["amt_schema_ccy"]);
            Assert.Equal(30m, row["amt_missing_ccy"]);
            Assert.Equal(6m, row["wt_field"]);
            Assert.Equal(11m, row["up_field"]);
            Assert.Contains("amt_field_ccy", changed);
        }

        [Fact]
        [DisplayName("ApplyComputedRow：formTable.Fields 為 null 時回傳空清單")]
        public void ApplyComputedRow_NullFields_ReturnsEmpty()
        {
            var schema = new FormSchema("Order", "Order");
            var formTable = schema.Tables!.Add("Order", "Order");
            formTable.SetSerializeState(SerializeState.Serialize);   // Fields → null
            var table = new DataTable("Order");
            table.Columns.Add("a", typeof(string));
            var row = table.NewRow();
            table.Rows.Add(row);

            var result = _calculator.ApplyComputedRow(schema, formTable, row, new RoundingContext());

            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("ApplyDefaultRow：空欄以字串常數運算式填入並回報；DBNull 目標保持空")]
        public void ApplyDefaultRow_FillsEmptyStringColumn()
        {
            var schema = new FormSchema("Order", "Order");
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add(new FormField("code", "Code", FieldDbType.String)
            {
                DefaultValueExpression = "\"AUTO\"",
            });

            var dataTable = new DataTable("Order");
            dataTable.Columns.Add("code", typeof(string));
            var row = dataTable.NewRow();
            row["code"] = string.Empty;   // 空字串 → IsEmptyValue 為真
            dataTable.Rows.Add(row);

            var changed = _calculator.ApplyDefaultRow(schema.MasterTable!, row);

            Assert.Equal("AUTO", row["code"]);
            Assert.Contains("code", changed);
        }

        [Fact]
        [DisplayName("ApplyDefaultRow：formTable.Fields 為 null 時回傳空清單")]
        public void ApplyDefaultRow_NullFields_ReturnsEmpty()
        {
            var schema = new FormSchema("Order", "Order");
            var formTable = schema.Tables!.Add("Order", "Order");
            formTable.SetSerializeState(SerializeState.Serialize);   // Fields → null
            var table = new DataTable("Order");
            table.Columns.Add("a", typeof(string));
            var row = table.NewRow();
            table.Rows.Add(row);

            var result = _calculator.ApplyDefaultRow(formTable, row);

            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("BuildDependencyMap：formTable.Fields 為 null 時回傳空對映")]
        public void BuildDependencyMap_NullFields_ReturnsEmpty()
        {
            var schema = new FormSchema("Order", "Order");
            var formTable = schema.Tables!.Add("Order", "Order");
            formTable.SetSerializeState(SerializeState.Serialize);   // Fields → null

            var map = _calculator.BuildDependencyMap(formTable);

            Assert.Empty(map);
        }
    }
}
