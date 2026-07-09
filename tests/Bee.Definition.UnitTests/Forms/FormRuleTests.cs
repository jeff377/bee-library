using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// PR1（定義層）測試：<see cref="FormField.ValueExpression"/> /
    /// <see cref="FormField.DefaultValueExpression"/> 與 <see cref="FormRule"/> /
    /// <see cref="FormSchema.Rules"/> 的 XML 序列化往返、空值省略、以及 Clone 深拷貝。
    /// FormSchema 以 XML 為唯一傳輸序列化路徑（後端 XmlCodec.Serialize → 前端
    /// XmlCodec.Deserialize），故不含 JSON / MessagePack 往返。
    /// </summary>
    public class FormRuleTests
    {
        #region FormField expression properties

        [Fact]
        [DisplayName("FormField ValueExpression 應透過 XmlAttribute 序列化往返")]
        public void ValueExpression_RoundTripsThroughXml()
        {
            var field = new FormField("amount", "金額", FieldDbType.Currency)
            {
                ValueExpression = "unit_price * qty",
            };

            var xml = XmlCodec.Serialize(field);
            var restored = XmlCodec.Deserialize<FormField>(xml);

            Assert.NotNull(restored);
            Assert.Equal("unit_price * qty", restored!.ValueExpression);
            Assert.Contains("ValueExpression=\"unit_price * qty\"", xml);
        }

        [Fact]
        [DisplayName("FormField DefaultValueExpression 應透過 XmlAttribute 序列化往返")]
        public void DefaultValueExpression_RoundTripsThroughXml()
        {
            var field = new FormField("order_date", "訂單日期", FieldDbType.DateTime)
            {
                DefaultValueExpression = "Today()",
            };

            var xml = XmlCodec.Serialize(field);
            var restored = XmlCodec.Deserialize<FormField>(xml);

            Assert.NotNull(restored);
            Assert.Equal("Today()", restored!.DefaultValueExpression);
        }

        [Fact]
        [DisplayName("FormField 運算式屬性為空預設值時序列化應省略屬性")]
        public void ExpressionProperties_Empty_OmitXmlAttributes()
        {
            var field = new FormField("sys_name", "名稱", FieldDbType.String);

            var xml = XmlCodec.Serialize(field);

            Assert.DoesNotContain("ValueExpression=", xml);
            Assert.DoesNotContain("DefaultValueExpression=", xml);
        }

        #endregion

        #region FormRule serialization

        [Fact]
        [DisplayName("FormRule 所有屬性應透過 XmlAttribute 序列化往返")]
        public void FormRule_RoundTripsThroughXml()
        {
            var rule = new FormRule("amount_positive", "amount > 0", "已核准訂單金額必須大於 0")
            {
                Trigger = FormRuleTrigger.BeforeSave,
                TargetTable = "OrderDetail",
                When = "status == \"Approved\"",
                Enabled = true,
                Order = 5,
            };

            var xml = XmlCodec.Serialize(rule);
            var restored = XmlCodec.Deserialize<FormRule>(xml);

            Assert.NotNull(restored);
            Assert.Equal("amount_positive", restored!.RuleId);
            Assert.Equal(FormRuleTrigger.BeforeSave, restored.Trigger);
            Assert.Equal("OrderDetail", restored.TargetTable);
            Assert.Equal("status == \"Approved\"", restored.When);
            Assert.Equal("amount > 0", restored.Condition);
            Assert.Equal("已核准訂單金額必須大於 0", restored.Message);
            Assert.True(restored.Enabled);
            Assert.Equal(5, restored.Order);
        }

        [Fact]
        [DisplayName("FormRule BeforeDelete trigger 應序列化往返")]
        public void FormRule_BeforeDeleteTrigger_RoundTripsThroughXml()
        {
            var rule = new FormRule("no_delete_closed", "status != \"Closed\"", "已結案不可刪除")
            {
                Trigger = FormRuleTrigger.BeforeDelete,
            };

            var xml = XmlCodec.Serialize(rule);
            var restored = XmlCodec.Deserialize<FormRule>(xml);

            Assert.NotNull(restored);
            Assert.Equal(FormRuleTrigger.BeforeDelete, restored!.Trigger);
        }

        [Fact]
        [DisplayName("FormRule When 為空時序列化應省略屬性（一律套用）")]
        public void FormRule_EmptyWhen_OmitsXmlAttribute()
        {
            var rule = new FormRule("always", "amount > 0", "金額必須大於 0");

            var xml = XmlCodec.Serialize(rule);

            Assert.DoesNotContain("When=", xml);
        }

        #endregion

        #region FormSchema.Rules

        [Fact]
        [DisplayName("FormSchema.Rules 新建時為空、序列化不輸出 Rules 節點")]
        public void Rules_Empty_OmittedFromXml()
        {
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };

            var xml = XmlCodec.Serialize(schema);

            Assert.DoesNotContain("<Rules", xml);
        }

        [Fact]
        [DisplayName("FormSchema.Rules 應透過 XML 序列化往返")]
        public void Rules_RoundTripThroughXml()
        {
            var schema = BuildSchemaWithRule();

            var xml = XmlCodec.Serialize(schema);
            var restored = XmlCodec.Deserialize<FormSchema>(xml);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.Rules);
            Assert.Single(restored.Rules!);
            var rule = restored.Rules!["amount_positive"];
            Assert.Equal("amount > 0", rule.Condition);
            Assert.Equal("金額必須大於 0", rule.Message);
        }

        [Fact]
        [DisplayName("FormSchema.Clone 應 deep-copy Rules，每個 entry 都是獨立實例")]
        public void Clone_CopiesRules()
        {
            var source = BuildSchemaWithRule();

            var clone = source.Clone();

            Assert.NotNull(clone.Rules);
            Assert.Single(clone.Rules!);
            Assert.NotSame(source.Rules, clone.Rules);
            Assert.NotSame(source.Rules!["amount_positive"], clone.Rules!["amount_positive"]);
            Assert.Equal("amount > 0", clone.Rules!["amount_positive"].Condition);

            // mutate clone 不影響來源
            clone.Rules!["amount_positive"].Message = "changed";
            Assert.Equal("金額必須大於 0", source.Rules!["amount_positive"].Message);
        }

        [Fact]
        [DisplayName("FormSchema.Clone 應 deep-copy FormField.ValueExpression")]
        public void Clone_CopiesFieldValueExpression()
        {
            var source = BuildSchemaWithRule();

            var clone = source.Clone();
            var field = clone.Tables!["Order"].Fields!["amount"];

            Assert.Equal("unit_price * qty", field.ValueExpression);
            Assert.NotSame(source.Tables!["Order"].Fields!["amount"], field);
        }

        #endregion

        private static FormSchema BuildSchemaWithRule()
        {
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };
            var table = schema.Tables!.Add("Order", "訂單");
            table.Fields!.Add("unit_price", "單價", FieldDbType.Currency);
            table.Fields!.Add("qty", "數量", FieldDbType.Decimal);
            table.Fields!.Add(new FormField("amount", "金額", FieldDbType.Currency)
            {
                ValueExpression = "unit_price * qty",
                ReadOnly = true,
            });
            schema.Rules!.Add("amount_positive", "amount > 0", "金額必須大於 0");
            return schema;
        }
    }
}
