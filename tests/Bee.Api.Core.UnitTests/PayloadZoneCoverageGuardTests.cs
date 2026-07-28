using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Base.Data;
using Bee.Definition.Filters;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 守門測試：<see cref="PayloadZoneConverter"/> 以「列舉具體型別」的 switch 決定要轉換什麼，
    /// 其型別註解也自承「新增 message 型別不會自動覆蓋」。本測試把那句警告變成會紅的測試——
    /// 反射掃出 <c>Bee.Api.Core.Messages.*</c> 內所有承載 <c>DataSet</c> / <c>DataTable</c> /
    /// <c>FilterNode</c> 的 message 型別，逐一實際跑一次轉換並斷言值真的位移了。
    /// </summary>
    /// <remarks>
    /// 這比「維護一份型別名單」強：名單只能證明有人記得改名單，本測試證明轉換真的發生。
    /// 新增一個帶資料的 message 型別卻忘了接進 switch，這裡會直接失敗並指名該型別。
    /// </remarks>
    public class PayloadZoneCoverageGuardTests
    {
        private const string Taipei = "Asia/Taipei";
        private static readonly DateTime Utc9Am = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

        /// <summary>掃出所有承載時間資料的 message 型別，連同其承載屬性。</summary>
        public static TheoryData<string> CarrierTypeNames()
        {
            var data = new TheoryData<string>();
            foreach (var type in CarrierTypes())
            {
                data.Add(type.FullName!);
            }
            return data;
        }

        private static IEnumerable<Type> CarrierTypes() =>
            typeof(ApiMessageBase).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract
                            && t.Namespace?.StartsWith("Bee.Api.Core.Messages", StringComparison.Ordinal) == true
                            && t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Any(IsTimeCarrier))
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

        private static bool IsTimeCarrier(PropertyInfo p) =>
            p.CanRead && p.CanWrite
            && (p.PropertyType == typeof(DataSet)
                || p.PropertyType == typeof(DataTable)
                || typeof(FilterNode).IsAssignableFrom(p.PropertyType));

        [Fact]
        [DisplayName("掃描應找到承載時間資料的 message 型別（掃描本身失效時要看得出來）")]
        public void CarrierScan_FindsTypes()
        {
            Assert.NotEmpty(CarrierTypes());
        }

        [Theory]
        [MemberData(nameof(CarrierTypeNames))]
        [DisplayName("每個承載時間資料的 message 型別都必須被 PayloadZoneConverter 實際轉換")]
        public void EveryCarrierType_IsActuallyConverted(string typeName)
        {
            var type = CarrierTypes().Single(t => t.FullName == typeName);
            var instance = Activator.CreateInstance(type)!;

            var carrier = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .First(IsTimeCarrier);
            carrier.SetValue(instance, BuildPayload(carrier.PropertyType));

            bool isRequest = typeof(ApiRequest).IsAssignableFrom(type);
            if (isRequest)
            {
                using var swap = PayloadZoneConverter.ToUtc(instance, Taipei);
                AssertShifted(carrier.GetValue(instance), typeName, toUtc: true);
            }
            else
            {
                PayloadZoneConverter.ToUserZone(instance, Taipei);
                AssertShifted(carrier.GetValue(instance), typeName, toUtc: false);
            }
        }

        private static object BuildPayload(Type carrierType)
        {
            if (typeof(FilterNode).IsAssignableFrom(carrierType))
            {
                return new FilterCondition("created_at", ComparisonOperator.Equal, Utc9Am);
            }

            var table = new DataTable("t");
            table.AddColumn("created_at", FieldDbType.DateTime);
            table.Rows.Add(Utc9Am);
            table.AcceptChanges();

            if (carrierType == typeof(DataTable)) { return table; }

            var dataSet = new DataSet();
            dataSet.Tables.Add(table);
            return dataSet;
        }

        private static void AssertShifted(object? converted, string typeName, bool toUtc)
        {
            var expected = Expected(toUtc);
            var actual = converted switch
            {
                DataSet ds => (DateTime)ds.Tables[0]!.Rows[0]["created_at"],
                DataTable dt => (DateTime)dt.Rows[0]["created_at"],
                FilterCondition condition => (DateTime)condition.Value!,
                _ => throw new InvalidOperationException($"Unhandled carrier shape on '{typeName}'.")
            };

            Assert.True(expected == actual,
                $"'{typeName}' 未被 PayloadZoneConverter 轉換（值仍為 {actual:O}，" +
                $"預期 {expected:O}）。新增帶資料的 message 型別時，請一併接進該類別的 switch。");
        }

        private static DateTime Expected(bool toUtc)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(Taipei);
            var shifted = toUtc
                ? TimeZoneInfo.ConvertTimeToUtc(Utc9Am, zone)
                : TimeZoneInfo.ConvertTimeFromUtc(Utc9Am, zone);
            return DateTime.SpecifyKind(shifted, DateTimeKind.Unspecified);
        }
    }
}
