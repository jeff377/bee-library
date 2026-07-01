using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Layouts
{
    /// <summary>
    /// LayoutColumnFactory 單元測試（共用 helper）。
    /// </summary>
    public class LayoutColumnFactoryTests
    {
        [Theory]
        [InlineData(ControlType.TextEdit, FieldDbType.String, ControlType.TextEdit)]
        [InlineData(ControlType.CheckEdit, FieldDbType.String, ControlType.CheckEdit)]
        [InlineData(ControlType.MemoEdit, FieldDbType.String, ControlType.MemoEdit)]
        [InlineData(ControlType.DropDownEdit, FieldDbType.Integer, ControlType.DropDownEdit)]
        [DisplayName("ResolveControlType 非 Auto 時應原樣回傳指定值")]
        public void ResolveControlType_NonAuto_ReturnsAsIs(ControlType type, FieldDbType dbType, ControlType expected)
        {
            var actual = LayoutColumnFactory.ResolveControlType(type, dbType);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(FieldDbType.Boolean, ControlType.CheckEdit)]
        [InlineData(FieldDbType.DateTime, ControlType.DateEdit)]
        [InlineData(FieldDbType.Text, ControlType.MemoEdit)]
        [InlineData(FieldDbType.String, ControlType.TextEdit)]
        [InlineData(FieldDbType.Short, ControlType.NumericEdit)]
        [InlineData(FieldDbType.Integer, ControlType.NumericEdit)]
        [InlineData(FieldDbType.Long, ControlType.NumericEdit)]
        [InlineData(FieldDbType.Decimal, ControlType.NumericEdit)]
        [InlineData(FieldDbType.Currency, ControlType.NumericEdit)]
        [InlineData(FieldDbType.Guid, ControlType.TextEdit)]
        [DisplayName("ResolveControlType Auto 時應依 DbType 推導預設控制型態")]
        public void ResolveControlType_Auto_MapsDbType(FieldDbType dbType, ControlType expected)
        {
            var actual = LayoutColumnFactory.ResolveControlType(ControlType.Auto, dbType);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [DisplayName("ToField 應將 FormField 屬性複製至 LayoutField")]
        public void ToField_CopiesProperties()
        {
            var formField = new FormField("amount", "金額", FieldDbType.Decimal)
            {
                ControlType = ControlType.TextEdit,
                DisplayFormat = "{0:C}",
                NumberFormat = "Amount"
            };

            var field = LayoutColumnFactory.ToField(formField);

            Assert.Equal("amount", field.FieldName);
            Assert.Equal("金額", field.Caption);
            Assert.Equal(ControlType.TextEdit, field.ControlType);
            Assert.Equal("{0:C}", field.DisplayFormat);
            Assert.Equal("Amount", field.NumberFormat);
        }

        [Fact]
        [DisplayName("ToColumn 應將 FormField 屬性（含 Width）複製至 LayoutColumn")]
        public void ToColumn_CopiesProperties()
        {
            var formField = new FormField("amount", "金額", FieldDbType.Decimal)
            {
                ControlType = ControlType.TextEdit,
                Width = 150,
                DisplayFormat = "{0:C}",
                NumberFormat = "Amount"
            };

            var column = LayoutColumnFactory.ToColumn(formField);

            Assert.Equal("amount", column.FieldName);
            Assert.Equal("金額", column.Caption);
            Assert.Equal(ControlType.TextEdit, column.ControlType);
            Assert.Equal(150, column.Width);
            Assert.Equal("{0:C}", column.DisplayFormat);
            Assert.Equal("Amount", column.NumberFormat);
        }

        [Fact]
        [DisplayName("ToField ControlType=Auto + DbType=Boolean 應推導為 CheckEdit")]
        public void ToField_AutoControlType_BooleanDbType_ProducesCheckEdit()
        {
            var formField = new FormField("active", "啟用", FieldDbType.Boolean)
            {
                ControlType = ControlType.Auto
            };

            var field = LayoutColumnFactory.ToField(formField);

            Assert.Equal(ControlType.CheckEdit, field.ControlType);
        }

        [Fact]
        [DisplayName("ToField/ToColumn 應傳遞 ReadOnly 與 Required 旗標")]
        public void ToFieldAndColumn_PropagateReadOnlyAndRequired()
        {
            var formField = new FormField("amount", "金額", FieldDbType.Decimal)
            {
                ReadOnly = true,
                Required = true,
            };

            var field = LayoutColumnFactory.ToField(formField);
            var column = LayoutColumnFactory.ToColumn(formField);

            Assert.True(field.ReadOnly);
            Assert.True(field.Required);
            Assert.True(column.ReadOnly);
            Assert.True(column.Required);
        }

        [Fact]
        [DisplayName("ToField/ToColumn 應傳遞 NumberKind 語意型別")]
        public void ToFieldAndColumn_PropagateNumberKind()
        {
            var formField = new FormField("amount", "金額", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Amount,
            };

            var field = LayoutColumnFactory.ToField(formField);
            var column = LayoutColumnFactory.ToColumn(formField);

            Assert.Equal(NumberKind.Amount, field.NumberKind);
            Assert.Equal(NumberKind.Amount, column.NumberKind);
        }

        [Fact]
        [DisplayName("ToField/ToColumn 應傳遞 CurrencyField（CUKY 參照欄名）")]
        public void ToFieldAndColumn_PropagateCurrencyField()
        {
            var formField = new FormField("home_amount", "本幣金額", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "local_currency",
            };

            var field = LayoutColumnFactory.ToField(formField);
            var column = LayoutColumnFactory.ToColumn(formField);

            Assert.Equal("local_currency", field.CurrencyField);
            Assert.Equal("local_currency", column.CurrencyField);
        }

        [Fact]
        [DisplayName("ToColumn Width=0 應保留 0 表示 auto/未設")]
        public void ToColumn_WidthZero_StaysZero()
        {
            var formField = new FormField("col", "欄", FieldDbType.String);

            var column = LayoutColumnFactory.ToColumn(formField);

            Assert.Equal(0, column.Width);
        }

        [Fact]
        [DisplayName("ResolveControlType Auto + RelationProgId 應解析為 ButtonEdit")]
        public void ResolveControlType_AutoWithRelation_ProducesButtonEdit()
        {
            var formField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
            };

            var actual = LayoutColumnFactory.ResolveControlType(formField);

            Assert.Equal(ControlType.ButtonEdit, actual);
        }

        [Fact]
        [DisplayName("ResolveControlType 顯式 ControlType 應優先於 RelationProgId 推導")]
        public void ResolveControlType_ExplicitTypeWithRelation_ExplicitWins()
        {
            var formField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                ControlType = ControlType.DropDownEdit,
                RelationProgId = "Customer",
            };

            var actual = LayoutColumnFactory.ResolveControlType(formField);

            Assert.Equal(ControlType.DropDownEdit, actual);
        }

        [Fact]
        [DisplayName("GetDisplayFields 顯式宣告應優先於慣例推導")]
        public void GetDisplayFields_Explicit_WinsOverConvention()
        {
            var formField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
                DisplayFields = "ref_customer_name",
            };
            formField.RelationFieldMappings!.Add("sys_id", "ref_customer_id");
            formField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");

            var actual = formField.GetDisplayFields();

            Assert.Equal(["ref_customer_name"], actual);
        }

        [Fact]
        [DisplayName("GetDisplayFields 未設時依慣例取 sys_id 與 sys_name 的目的欄（編號+名稱）")]
        public void GetDisplayFields_Convention_UsesIdAndNameMappings()
        {
            var formField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
            };
            formField.RelationFieldMappings!.Add("sys_id", "ref_customer_id");
            formField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");

            var actual = formField.GetDisplayFields();

            Assert.Equal(["ref_customer_id", "ref_customer_name"], actual);
        }

        [Fact]
        [DisplayName("GetDisplayFields 交易型目標只映射 sys_id 時應只回傳編號欄（單號顯示）")]
        public void GetDisplayFields_IdMappingOnly_ReturnsIdField()
        {
            var formField = new FormField("po_rowid", "採購單", FieldDbType.Guid)
            {
                RelationProgId = "PurchaseOrder",
            };
            formField.RelationFieldMappings!.Add("sys_id", "ref_po_no");

            var actual = formField.GetDisplayFields();

            Assert.Equal(["ref_po_no"], actual);
        }

        [Fact]
        [DisplayName("GetDisplayFields 非 relation 欄位應回傳空集合")]
        public void GetDisplayFields_NonRelationField_ReturnsEmpty()
        {
            var formField = new FormField("amount", "金額", FieldDbType.Decimal);

            var actual = formField.GetDisplayFields();

            Assert.Empty(actual);
        }

        [Fact]
        [DisplayName("ToField relation 欄位應帶 ButtonEdit 與慣例 DisplayFields")]
        public void ToField_RelationField_CarriesButtonEditAndDisplayField()
        {
            var formField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
            };
            formField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");

            var field = LayoutColumnFactory.ToField(formField);

            Assert.Equal(ControlType.ButtonEdit, field.ControlType);
            Assert.Equal("ref_customer_name", field.DisplayFields);
        }

        [Fact]
        [DisplayName("ToColumn relation 欄位應帶 ButtonEdit 與慣例 DisplayFields")]
        public void ToColumn_RelationField_CarriesButtonEditAndDisplayField()
        {
            var formField = new FormField("product_rowid", "商品", FieldDbType.Guid)
            {
                RelationProgId = "Product",
            };
            formField.RelationFieldMappings!.Add("sys_name", "ref_product_name");

            var column = LayoutColumnFactory.ToColumn(formField);

            Assert.Equal(ControlType.ButtonEdit, column.ControlType);
            Assert.Equal("ref_product_name", column.DisplayFields);
        }
    }
}
