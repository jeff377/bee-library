using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// FormField 未覆蓋路徑測試：Table 屬性的 Collection==null 分支、
    /// 歸屬 FormTable 後取得 Owner、ToString 格式。
    /// </summary>
    public class FormFieldTests
    {
        [Fact]
        [DisplayName("Table 於 Collection 為 null 時應回傳 null")]
        public void Table_NoCollection_ReturnsNull()
        {
            var field = new FormField("sys_no", "流水號", FieldDbType.AutoIncrement);

            // 尚未加入任何 FormFieldCollection,Collection 為 null
            Assert.Null(field.Table);
        }

        [Fact]
        [DisplayName("Table 於加入 FormTable.Fields 後應回傳該 FormTable")]
        public void Table_AddedToFormTable_ReturnsOwner()
        {
            var ft = new FormTable("Customer", "客戶");
            var field = new FormField("sys_no", "流水號", FieldDbType.AutoIncrement);
            ft.Fields!.Add(field);

            Assert.Same(ft, field.Table);
        }

        [Fact]
        [DisplayName("ToString 應回傳 \"FieldName - Caption\"")]
        public void ToString_ReturnsFieldNameDashCaption()
        {
            var field = new FormField("sys_no", "流水號", FieldDbType.AutoIncrement);

            Assert.Equal("sys_no - 流水號", field.ToString());
        }

        [Fact]
        [DisplayName("NumberKind 預設為 None")]
        public void NumberKind_DefaultsToNone()
        {
            var field = new FormField("amount", "金額", FieldDbType.Decimal);

            Assert.Equal(NumberKind.None, field.NumberKind);
        }

        [Fact]
        [DisplayName("NumberKind XML round-trip 應保留語意型別")]
        public void NumberKind_XmlRoundtrip_Preserved()
        {
            var original = new FormField("unit_price", "單價", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.UnitPrice,
            };

            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<FormField>(xml);

            Assert.NotNull(restored);
            Assert.Equal(NumberKind.UnitPrice, restored!.NumberKind);
        }

        [Fact]
        [DisplayName("NumberKind=None 為預設值時序列化應省略屬性")]
        public void NumberKind_None_OmitsXmlAttribute()
        {
            var field = new FormField("col", "欄", FieldDbType.String);

            var xml = XmlCodec.Serialize(field);

            Assert.DoesNotContain("NumberKind=", xml);
        }

        [Fact]
        [DisplayName("Clone 應複製 NumberKind、ReadOnly、Required")]
        public void Clone_CopiesNumberKindReadOnlyRequired()
        {
            var original = new FormField("amount", "金額", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Amount,
                ReadOnly = true,
                Required = true,
            };

            var clone = original.Clone();

            Assert.Equal(NumberKind.Amount, clone.NumberKind);
            Assert.True(clone.ReadOnly);
            Assert.True(clone.Required);
        }

        [Fact]
        [DisplayName("CurrencyField XML round-trip 應保留 CUKY 參照欄名")]
        public void CurrencyField_XmlRoundtrip_Preserved()
        {
            var original = new FormField("home_amount", "本幣金額", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "local_currency",
            };

            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<FormField>(xml);

            Assert.NotNull(restored);
            Assert.Equal("local_currency", restored!.CurrencyField);
        }

        [Fact]
        [DisplayName("CurrencyField 為空預設值時序列化應省略屬性")]
        public void CurrencyField_Empty_OmitsXmlAttribute()
        {
            var field = new FormField("col", "欄", FieldDbType.String);

            var xml = XmlCodec.Serialize(field);

            Assert.DoesNotContain("CurrencyField=", xml);
        }

        [Fact]
        [DisplayName("Clone 應複製 CurrencyField")]
        public void Clone_CopiesCurrencyField()
        {
            var original = new FormField("home_amount", "本幣金額", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "local_currency",
            };

            var clone = original.Clone();

            Assert.Equal("local_currency", clone.CurrencyField);
        }

        [Fact]
        [DisplayName("UnitField XML round-trip 應保留 UNIT 參照欄名")]
        public void UnitField_XmlRoundtrip_Preserved()
        {
            var original = new FormField("order_qty", "數量", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Quantity,
                UnitField = "qty_uom",
            };

            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<FormField>(xml);

            Assert.NotNull(restored);
            Assert.Equal("qty_uom", restored!.UnitField);
        }

        [Fact]
        [DisplayName("UnitField 為空預設值時序列化應省略屬性")]
        public void UnitField_Empty_OmitsXmlAttribute()
        {
            var field = new FormField("col", "欄", FieldDbType.String);

            var xml = XmlCodec.Serialize(field);

            Assert.DoesNotContain("UnitField=", xml);
        }

        [Fact]
        [DisplayName("Clone 應複製 UnitField")]
        public void Clone_CopiesUnitField()
        {
            var original = new FormField("order_qty", "數量", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Quantity,
                UnitField = "qty_uom",
            };

            var clone = original.Clone();

            Assert.Equal("qty_uom", clone.UnitField);
        }
    }
}
