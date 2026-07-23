using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages;
using Bee.Base.Serialization;

namespace Bee.Api.Core.UnitTests.Contracts
{
    /// <summary>
    /// 對**所有** API 合約型別（<see cref="ApiRequest"/>／<see cref="ApiResponse"/> 子型別）驗證能以
    /// **MessagePack 與 JSON 兩種** wire format 序列化並保真往返。反射列舉組件,自動涵蓋現有與未來合約 ——
    /// 任何合約異動只要破壞其中一種格式的傳遞（新增不支援的屬性型別、[Key] 對調、屬性名不對稱、
    /// 移除無參數建構子等）即會失敗。
    /// </summary>
    /// <remarks>
    /// 做法對齊 sibling repo 的 SoarCloud.Api.Core.Tests/Transformers/ApiContractSerializationTests。
    /// 兩個 serializer 策略對齊 bee wire 真實路徑:MessagePack 走 <see cref="MessagePackCodec"/>
    /// （含 SafeMessagePackSerializerOptions + 自訂 formatter + resolver 鏈）、JSON 走
    /// <see cref="JsonCodec"/>（含 DataSet/DataTable converter、camelCase、enum-as-string、
    /// IObjectSerialize 生命週期 hook）。計畫見 docs/plans/plan-api-contract-serialization-tests.md。
    /// </remarks>
    public class ApiContractSerializationTests
    {
        private static readonly Guid SampleGuid = new("11111111-2222-3333-4444-555555555555");
        private static readonly DateTime SampleUtc = new(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTimeOffset SampleOffset = new(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);

        // JsonCodec 只有泛型 Deserialize<T>(string, bool),反射掃型別時以 MakeGenericMethod 呼叫。
        private static readonly MethodInfo JsonDeserializeGeneric = typeof(JsonCodec)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(JsonCodec.Deserialize)
                && m.IsGenericMethodDefinition
                && m.GetParameters() is [{ ParameterType.FullName: "System.String" }, ..]);

        // 兩種 wire 序列化策略:合約異動須在兩者皆能保真傳遞。統一以 byte[] 為比對單位。
        private static readonly IReadOnlyDictionary<string, SerializerStrategy> Strategies =
            new Dictionary<string, SerializerStrategy>(StringComparer.Ordinal)
            {
                ["msgpack"] = new SerializerStrategy(
                    (obj, type) => MessagePackCodec.Serialize(obj, type),
                    (bytes, type) => MessagePackCodec.Deserialize(bytes, type)),
                ["json"] = new SerializerStrategy(
                    (obj, _) => Encoding.UTF8.GetBytes(JsonCodec.Serialize(obj)),
                    (bytes, type) => JsonDeserializeGeneric
                        .MakeGenericMethod(type)
                        .Invoke(null, [Encoding.UTF8.GetString(bytes), true])),
            };

        public static TheoryData<string, Type> Cases()
        {
            var data = new TheoryData<string, Type>();
            var contracts = typeof(ApiRequest).Assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsClass: true }
                    && (typeof(ApiRequest).IsAssignableFrom(t) || typeof(ApiResponse).IsAssignableFrom(t)))
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

            foreach (var type in contracts)
            {
                foreach (var strategy in Strategies.Keys)
                {
                    data.Add(strategy, type);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Cases))]
        [DisplayName("API 合約型別經 MessagePack/JSON 序列化應保真往返")]
        public void Contract_SerializesAndRoundTrips(string serializerName, Type type)
        {
            var strategy = Strategies[serializerName];
            var instance = Activator.CreateInstance(type)!;
            Populate(instance, depth: 0);

            var bytes = strategy.Serialize(instance, type);
            var restored = strategy.Deserialize(bytes, type);

            Assert.NotNull(restored);
            Assert.IsType(type, restored);

            // 保真:決定性序列化下,還原後再序列化應得相同 bytes（值無遺失）。
            Assert.Equal(bytes, strategy.Serialize(restored!, type));
        }

        /// <summary>
        /// 以樣本非預設值填滿可寫（public setter）scalar 屬性,讓保真檢查有意義;
        /// 巢狀 class 遞歸一層填 scalar,集合／字典／DataSet 維持預設。
        /// </summary>
        private static void Populate(object instance, int depth)
        {
            foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 只填 public getter + public setter;排除 SerializeState（private setter）等。
                if (property.GetMethod is not { IsPublic: true } || property.SetMethod is not { IsPublic: true })
                {
                    continue;
                }

                var value = SampleValue(property.PropertyType, depth);
                if (value is not null)
                {
                    property.SetValue(instance, value);
                }
            }
        }

        private static object? SampleValue(Type type, int depth)
        {
            var target = Nullable.GetUnderlyingType(type) ?? type;

            if (target == typeof(string)) { return "sample"; }
            if (target == typeof(Guid)) { return SampleGuid; }
            if (target == typeof(bool)) { return true; }
            if (target == typeof(int)) { return 7; }
            if (target == typeof(long)) { return 7L; }
            if (target == typeof(short)) { return (short)7; }
            if (target == typeof(decimal)) { return 7.5m; }
            if (target == typeof(double)) { return 7.5d; }
            if (target == typeof(DateTime)) { return SampleUtc; }
            if (target == typeof(DateTimeOffset)) { return SampleOffset; }
            if (target == typeof(byte[])) { return new byte[] { 1, 2, 3, 4 }; }

            if (target.IsEnum)
            {
                // 取第一個非零成員（若有）,否則預設,讓保真檢查對 enum 有意義。
                var values = Enum.GetValues(target);
                return values.Length > 1 ? values.GetValue(1) : values.GetValue(0);
            }

            // DataSet / DataTable:盲目遞歸 Populate 會亂設 EnforceConstraints 等,脆弱且不可靠。
            // 其深度 round-trip 已由既有 Form/*MessagePackTests、AuditLog/* 與 *JsonRpcRoundTripTests
            // 以真實資料覆蓋,此處留 null,breadth 測試仍覆蓋該合約其餘 scalar 欄位。
            if (target == typeof(DataSet) || target == typeof(DataTable)) { return null; }

            // 集合／字典:維持預設（空）,避免泛型填值複雜度;空集合仍能保真往返。
            if (target != typeof(string) && typeof(IEnumerable).IsAssignableFrom(target)) { return null; }

            // 巢狀複雜型別:遞歸一層填 scalar,驗證物件圖也能序列化。
            if (target is { IsClass: true } && target != typeof(object)
                && depth < 2 && target.GetConstructor(Type.EmptyTypes) is not null)
            {
                var nested = Activator.CreateInstance(target)!;
                Populate(nested, depth + 1);
                return nested;
            }

            return null;
        }

        private sealed class SerializerStrategy
        {
            public SerializerStrategy(Func<object, Type, byte[]> serialize, Func<byte[], Type, object?> deserialize)
            {
                Serialize = serialize;
                Deserialize = deserialize;
            }

            public Func<object, Type, byte[]> Serialize { get; }

            public Func<byte[], Type, object?> Deserialize { get; }
        }
    }
}
