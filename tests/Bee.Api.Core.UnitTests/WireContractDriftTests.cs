using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text.Json.Serialization;
using Bee.Api.Core.MessagePack;
using Bee.Base.Collections;
using MessagePack.Formatters;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 守衛 wire 型別與其 formatter 註冊之間的漂移。
    /// </summary>
    /// <remarks>
    /// 編譯器不會把型別與 formatter 綁在一起：`Bee.Api.Core.Messages.*` 新增一個型別、
    /// 或既有 wire 型別新增一個屬性，都不會有任何東西提醒你去補註冊——桌面上 contractless
    /// 會默默接手，直到 iOS 上炸成 <c>FormatterNotRegisteredException</c> 或欄位靜默消失。
    /// <para>
    /// 本測試從與 <c>WireContracts</c> 相同的根走一次型別閉包，逐一比對註冊清單。
    /// 它是「必須顯式註冊」這條規則的唯一自動化把關。
    /// </para>
    /// </remarks>
    public class WireContractDriftTests
    {
        /// <summary>
        /// 不經任何訊息屬性抵達、但確實會上 wire 的型別（藏在 `object` 成員內，或以定義資料取得）。
        /// </summary>
        private static readonly Type[] ExtraRoots =
        [
            typeof(Bee.Definition.Collections.ListItemCollection),
            typeof(Bee.Definition.Collections.PropertyCollection),
            typeof(Bee.Definition.Settings.CurrencySettings),
            typeof(Bee.Definition.Settings.UnitSettings),
            typeof(SerializableDataSet),
            typeof(SerializableDataTable),
        ];

        [Fact]
        [DisplayName("wire 型別閉包內每個型別都必須有顯式註冊的 formatter")]
        public void WireTypeClosure_IsFullyRegistered()
        {
            var registered = RegisteredTypes();
            var missing = WireTypeClosure()
                .Where(t => !registered.Contains(t))
                .Select(t => t.FullName!)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            Assert.True(
                missing.Count == 0,
                $"以下 wire 型別沒有顯式 formatter，在 iOS（無動態碼）上會失敗：{Environment.NewLine}" +
                string.Join(Environment.NewLine, missing));
        }

        [Fact]
        [DisplayName("每個 WireContract 的成員清單必須與型別當下的形狀一致")]
        public void WireContracts_MatchTypeShape()
        {
            var drift = new List<string>();

            foreach (var contract in MessagePackCodec.RegisteredFormatters.OfType<IWireContract>())
            {
                var expected = WireMemberNames(contract.WireType);
                var actual = contract.WireMemberNames.ToList();

                var onlyOnType = expected.Except(actual, StringComparer.Ordinal).ToList();
                var onlyInContract = actual.Except(expected, StringComparer.Ordinal).ToList();

                if (onlyOnType.Count > 0)
                    drift.Add($"{contract.WireType.FullName}: 型別上有但未註冊 → {string.Join(", ", onlyOnType)}");
                if (onlyInContract.Count > 0)
                    drift.Add($"{contract.WireType.FullName}: 已註冊但型別上已無 → {string.Join(", ", onlyInContract)}");
            }

            Assert.True(
                drift.Count == 0,
                $"wire 合約與型別形狀不一致：{Environment.NewLine}{string.Join(Environment.NewLine, drift)}");
        }

        /// <summary>
        /// wire 成員的定義與 JSON 相同：public 可讀可寫、且未標 <c>[JsonIgnore]</c> 的屬性。
        /// 框架管理成員（<c>Tag</c> / <c>Key</c> / <c>SerializeState</c>）都帶著該標註，
        /// 因此自動被排除。
        /// </summary>
        private static List<string> WireMemberNames(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Where(p => p.GetMethod is { IsPublic: true } && p.SetMethod is { IsPublic: true })
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .Select(p => p.Name)
                .ToList();

        /// <summary>
        /// 每個註冊的 formatter 所涵蓋的型別（由 <c>IMessagePackFormatter&lt;T&gt;</c> 的 T 取得）。
        /// </summary>
        private static HashSet<Type> RegisteredTypes()
        {
            var types = new HashSet<Type>();
            foreach (var formatter in MessagePackCodec.RegisteredFormatters)
            {
                foreach (var i in formatter.GetType().GetInterfaces())
                {
                    if (!i.IsGenericType || i.GetGenericTypeDefinition() != typeof(IMessagePackFormatter<>))
                        continue;
                    var t = i.GetGenericArguments()[0];
                    types.Add(Nullable.GetUnderlyingType(t) ?? t);
                }
            }
            return types;
        }

        /// <summary>
        /// 從 API 訊息合約走出的型別閉包，回傳其中「需要顯式 formatter」的型別。
        /// </summary>
        private static HashSet<Type> WireTypeClosure()
        {
            var needs = new HashSet<Type>();
            var seen = new HashSet<Type>();
            var apiCore = typeof(MessagePackCodec).Assembly;
            var contracts = typeof(Bee.Api.Contracts.Form.IGetListRequest).Assembly;

            foreach (var asm in new[] { apiCore, contracts })
            {
                foreach (var t in asm.GetTypes())
                {
                    if (!t.IsClass || t.IsAbstract || t.IsGenericTypeDefinition) continue;
                    var ns = t.Namespace ?? string.Empty;
                    if (ns.StartsWith("Bee.Api.Core.Messages", StringComparison.Ordinal) ||
                        ns.StartsWith("Bee.Api.Contracts", StringComparison.Ordinal))
                    {
                        Visit(t);
                    }
                }
            }
            foreach (var t in ExtraRoots) Visit(t);

            return needs;

            void Visit(Type type)
            {
                var underlying = Nullable.GetUnderlyingType(type);
                if (underlying != null)
                {
                    needs.Add(underlying);
                    type = underlying;
                }
                if (!seen.Add(type)) return;

                if (type.IsEnum) { needs.Add(type); return; }
                if (IsBuiltIn(type)) return;
                if (type.IsArray)
                {
                    var element = type.GetElementType()!;
                    if (element != typeof(byte) && element != typeof(object)) needs.Add(type);
                    Visit(element);
                    return;
                }
                if (type == typeof(DataTable) || type == typeof(DataSet)) { needs.Add(type); return; }

                if (type.IsGenericType)
                {
                    var definition = type.GetGenericTypeDefinition();
                    if (definition == typeof(List<>) || definition == typeof(Dictionary<,>))
                    {
                        needs.Add(type);
                        foreach (var a in type.GetGenericArguments()) Visit(a);
                    }
                    return;
                }

                if (!type.IsClass) return;

                if (FrameworkCollectionItem(type) is { } item)
                {
                    needs.Add(type);
                    Visit(item);
                    return;
                }

                if (!type.IsAbstract) needs.Add(type);
                foreach (var name in WireMemberNames(type))
                    Visit(type.GetProperty(name)!.PropertyType);
                foreach (var derived in type.Assembly.GetTypes().Where(x => x.BaseType == type && !x.IsAbstract))
                    Visit(derived);
            }
        }

        private static Type? FrameworkCollectionItem(Type type)
        {
            for (var b = type.BaseType; b != null; b = b.BaseType)
            {
                if (!b.IsGenericType) continue;
                var d = b.GetGenericTypeDefinition();
                if (d == typeof(CollectionBase<>) || d == typeof(KeyCollectionBase<>))
                    return b.GetGenericArguments()[0];
            }
            return null;
        }

        private static bool IsBuiltIn(Type t) =>
            t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid) ||
            t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan) ||
            t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(object) ||
            t == typeof(byte[]) || t == typeof(Type) || t == typeof(Uri) || t == typeof(Version);
    }
}
