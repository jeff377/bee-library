using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Definitions;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Identity;

namespace Bee.Api.Client.UnitTests.Definitions
{
    /// <summary>
    /// <see cref="FormDefinitionLoader"/> 對數值格式的 bake 行為。
    /// </summary>
    /// <remarks>
    /// 定義類 API 一律供應「原樣儲存」的定義，公司位數的套用因此落在需求端。本組測試釘住
    /// 「loader 會 bake」這件事本身——這條線斷過一次：伺服端在 v4.x 把 bake 移除並註明改由
    /// 需求端處理，但需求端當時沒有補上，於是公司位數對任何 head 都不生效。
    /// <para>
    /// 用 fake connector 而非 <c>[DbFact]</c>：待驗的是 loader 的加工步驟，不是取得定義的傳輸。
    /// </para>
    /// </remarks>
    public class FormDefinitionLoaderNumberFormatTests
    {
        private const string ProgId = "Order";

        /// <summary>
        /// 每次 <c>GetDefine</c> 都回一份全新的 schema，避免測試之間共用實例而互相污染。
        /// </summary>
        private sealed class SchemaConnector : SystemApiConnector
        {
            public SchemaConnector() : base(Guid.NewGuid()) { }

            public override Task<T> GetDefineAsync<T>(DefineType defineType, string[]? keys = null)
            {
                if (defineType != DefineType.FormSchema)
                    return Task.FromResult(Activator.CreateInstance<T>());

                var schema = new FormSchema(ProgId, "訂單") { CurrencyField = "sys_currency" };
                var table = schema.Tables!.Add(ProgId, "訂單");
                table.Fields!.Add(new FormField("disc", "折扣", FieldDbType.Decimal) { NumberKind = NumberKind.Percent });
                table.Fields!.Add(new FormField("amount", "金額", FieldDbType.Decimal) { NumberKind = NumberKind.Amount });
                return Task.FromResult((T)(object)schema);
            }
        }

        private static FormDefinitionLoader CreateLoader(CompanyInfo? company)
            => new(new ClientDefineAccess(new SchemaConnector()))
            {
                CompanyAccessor = () => company,
            };

        private static CompanyInfo CompanyWithPercentDecimals(int decimals)
        {
            var company = new CompanyInfo { CompanyId = "C001" };
            company.NumberFormats.Add(new NumberFormatItem(NumberKind.Percent, decimals));
            return company;
        }

        [Fact]
        [DisplayName("GetLocalizedSchemaAsync 空語系路徑也會 bake 數值格式")]
        public async Task GetLocalizedSchemaAsync_BlankLang_BakesNumberFormat()
        {
            // 空語系是最常走到的路徑（CultureInfo.InvariantCulture.Name 就是空字串），
            // 而格式與語系無關，所以這條路徑不能略過 bake。
            var loader = CreateLoader(CompanyWithPercentDecimals(3));

            var schema = await loader.GetLocalizedSchemaAsync(ProgId, string.Empty);

            Assert.Equal("P3", schema.Tables![ProgId].Fields!["disc"].NumberFormat);
        }

        [Fact]
        [DisplayName("GetLocalizedSchemaAsync 未提供 CompanyAccessor 時 bake 框架預設格式")]
        public async Task GetLocalizedSchemaAsync_NoCompanyAccessor_BakesFrameworkDefault()
        {
            var loader = new FormDefinitionLoader(new ClientDefineAccess(new SchemaConnector()));

            var schema = await loader.GetLocalizedSchemaAsync(ProgId, string.Empty);

            Assert.Equal("P2", schema.Tables![ProgId].Fields!["disc"].NumberFormat);
        }

        [Fact]
        [DisplayName("GetLocalizedSchemaAsync 不同公司位數應得到不同格式")]
        public async Task GetLocalizedSchemaAsync_DifferentCompanies_DifferentFormats()
        {
            var schemaA = await CreateLoader(CompanyWithPercentDecimals(2)).GetLocalizedSchemaAsync(ProgId, string.Empty);
            var schemaB = await CreateLoader(CompanyWithPercentDecimals(4)).GetLocalizedSchemaAsync(ProgId, string.Empty);

            Assert.Equal("P2", schemaA.Tables![ProgId].Fields!["disc"].NumberFormat);
            Assert.Equal("P4", schemaB.Tables![ProgId].Fields!["disc"].NumberFormat);
        }

        [Fact]
        [DisplayName("GetLocalizedSchemaAsync 金額欄不 bake，但繼承主檔幣別欄供 UI 逐列解析")]
        public async Task GetLocalizedSchemaAsync_AmountField_InheritsMasterCurrencyField()
        {
            var loader = CreateLoader(CompanyWithPercentDecimals(2));

            var schema = await loader.GetLocalizedSchemaAsync(ProgId, string.Empty);

            var amount = schema.Tables![ProgId].Fields!["amount"];
            Assert.Equal(string.Empty, amount.NumberFormat);
            Assert.Equal("sys_currency", amount.CurrencyField);
        }

        [Fact]
        [DisplayName("CompanyAccessor 為委派：更換公司後同一個 loader 應改用新公司的位數")]
        public async Task CompanyAccessor_IsRead_PerCall()
        {
            // 委派而非值：EnterCompany / LeaveCompany 會在 session 存續期間換公司，
            // 建構時快照下來的公司會讓 loader 一直 bake 前一租戶的位數。
            CompanyInfo? current = CompanyWithPercentDecimals(2);
            var loader = new FormDefinitionLoader(new ClientDefineAccess(new SchemaConnector()))
            {
                CompanyAccessor = () => current,
            };

            var before = await loader.GetLocalizedSchemaAsync(ProgId, string.Empty);
            current = CompanyWithPercentDecimals(4);
            var after = await loader.GetLocalizedSchemaAsync(ProgId, string.Empty);

            Assert.Equal("P2", before.Tables![ProgId].Fields!["disc"].NumberFormat);
            Assert.Equal("P4", after.Tables![ProgId].Fields!["disc"].NumberFormat);
        }
    }
}
