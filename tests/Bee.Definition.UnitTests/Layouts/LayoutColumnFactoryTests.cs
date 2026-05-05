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
        [InlineData(FieldDbType.Integer, ControlType.TextEdit)]
        [InlineData(FieldDbType.Decimal, ControlType.TextEdit)]
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
        [DisplayName("ToColumn Width=0 應保留 0 表示 auto/未設")]
        public void ToColumn_WidthZero_StaysZero()
        {
            var formField = new FormField("col", "欄", FieldDbType.String);

            var column = LayoutColumnFactory.ToColumn(formField);

            Assert.Equal(0, column.Width);
        }
    }
}
