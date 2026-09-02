using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using Bee.Definition.Filters;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 守住每個宣告型別為 <see cref="FilterNode"/> 的屬性都掛了
    /// <see cref="FilterNodeJsonConverter"/>。
    /// </summary>
    /// <remarks>
    /// 少了標註不會有任何徵兆：System.Text.Json 綁宣告型別，於是整棵篩選子樹靜默消失，
    /// 既不擲例外也不留紀錄。編譯器不會提醒，型別本身也無從標註——converter 一旦標在
    /// <see cref="FilterNode"/> 上就會被子類繼承而無限遞迴（stack 爆掉、直接 segfault），
    /// 所以標註只能逐屬性下，而「逐屬性」正是會漏的那種規則。
    /// <para>
    /// 因此把它變成測試：新增一個 <see cref="FilterNode"/> 屬性卻忘了標註，這裡就會紅。
    /// </para>
    /// </remarks>
    public class FilterNodeConverterCoverageTests
    {
        [Fact]
        [DisplayName("每個 FilterNode 型別的屬性都必須標註 FilterNodeJsonConverter")]
        public void EveryFilterNodeProperty_DeclaresTheConverter()
        {
            var assemblies = new[]
            {
                typeof(Messages.Form.GetListRequest).Assembly,   // Bee.Api.Core
                typeof(FilterNode).Assembly,                     // Bee.Definition
                typeof(Bee.Business.BusinessObject).Assembly,    // Bee.Business
            };

            var properties = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(p => p.PropertyType == typeof(FilterNode))
                // 只看可寫入的：有 setter 才會參與反序列化，也才是 wire 上的持有者。
                // get-only 的（如 BO 執行期 context 的 DeleteContext.ScopeFilter，同一個物件
                // 還帶著 repository 介面）從不經過 JSON，標註它只會誤導讀者以為它上 wire。
                .Where(p => p.SetMethod?.IsPublic == true)
                .ToList();

            // 掃不到任何屬性代表掃描本身壞了（型別搬家、組件換名），而不是「全部合規」。
            Assert.NotEmpty(properties);

            var missing = properties
                .Where(p => p.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType
                            != typeof(FilterNodeJsonConverter))
                .Select(p => $"{p.DeclaringType!.FullName}.{p.Name}")
                .ToList();

            Assert.True(missing.Count == 0,
                "下列 FilterNode 屬性未標註 [JsonConverter(typeof(FilterNodeJsonConverter))]，" +
                "JSON 序列化時會靜默丟失整棵篩選子樹：" + global::System.Environment.NewLine +
                string.Join(global::System.Environment.NewLine, missing));
        }
    }
}
