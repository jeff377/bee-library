using System.Collections;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 由訊息型別產生 TypeScript 宣告，供另一個語言的 client 消費。
    /// </summary>
    /// <remarks>
    /// 手抄一份型別表在 TS 那端，就是同一份 API 合約的第二個權威來源——伺服端改了欄位名，
    /// 那份抄本不會知道，而漂掉的症狀是欄位靜默消失。改由這裡產生，型別表就成為衍生物。
    /// <para>
    /// 產生的是 <b>wire 形狀</b>而非 CLR 形狀：<c>Guid</c> 與 <c>DateTime</c> 在 JSON 上都是
    /// 字串，列舉是字串字面值聯集（<c>JsonStringEnumConverter</c>），<c>object</c> 成員則是
    /// 判別式封套。讀者要的是「這個 JSON 長什麼樣」，不是「C# 怎麼宣告」。
    /// </para>
    /// </remarks>
    internal static class WireContractGenerator
    {
        /// <summary>訊息型別所在的命名空間前綴。</summary>
        private const string MessageNamespace = "Bee.Api.Core.Messages";

        /// <summary>
        /// 手寫的前言：這幾個型別的 wire 形狀由自訂 converter 決定，反射看不出來。
        /// </summary>
        private const string Preamble = """
            // Generated from the Bee.NET message types — do not edit by hand.
            //
            // These describe the JSON shape on the wire, not the CLR declarations: a Guid and a
            // DateTime are both strings here, enums are string literal unions (the server writes
            // them with JsonStringEnumConverter), and an object-typed member is the discriminated
            // envelope this package calls a wire value.

            /** A value carrying its type discriminator: `[code, value]`. */
            export type WireValue = [number, unknown] | null;

            /** A column's shape inside a serialized DataTable. */
            export interface DataColumnShape {
              name: string;
              type: string;
              allowNull: boolean;
              readOnly: boolean;
              maxLength: number;
              caption: string;
              defaultValue: unknown;
            }

            /** A row, carrying its state and the versions that state implies. */
            export interface DataRowShape {
              state: 'Unchanged' | 'Added' | 'Modified' | 'Deleted';
              current?: Record<string, unknown>;
              original?: Record<string, unknown>;
            }

            export interface DataTable {
              tableName: string;
              columns: DataColumnShape[];
              primaryKeys: string[];
              rows: DataRowShape[];
            }

            export interface DataRelationShape {
              name: string;
              parentTable: string;
              childTable: string;
              parentColumns: string[];
              childColumns: string[];
            }

            export interface DataSet {
              dataSetName: string;
              tables: DataTable[];
              relations: DataRelationShape[];
            }
            """;

        /// <summary>
        /// 產生完整的 <c>.d.ts</c> 內容。
        /// </summary>
        public static string Generate()
        {
            var roots = typeof(Messages.ApiMessageBase).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Where(t => t.Namespace?.StartsWith(MessageNamespace, StringComparison.Ordinal) == true)
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

            var interfaces = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var enums = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var pending = new Queue<Type>(roots);
            var seen = new HashSet<Type>();

            while (pending.Count > 0)
            {
                var type = pending.Dequeue();
                if (!seen.Add(type)) continue;

                if (type.IsEnum)
                {
                    enums[type.Name] = RenderEnum(type);
                    continue;
                }

                interfaces[type.Name] = RenderInterface(type, pending);
            }

            var builder = new StringBuilder();
            builder.AppendLine(Preamble);
            builder.AppendLine();
            foreach (var body in enums.Values) builder.AppendLine(body);
            foreach (var body in interfaces.Values) builder.AppendLine(body);
            return builder.ToString().TrimEnd() + Environment.NewLine;
        }

        private static string RenderEnum(Type type)
        {
            var members = Enum.GetNames(type).Select(n => $"'{n}'");
            return $"export type {type.Name} = {string.Join(" | ", members)};{Environment.NewLine}";
        }

        private static string RenderInterface(Type type, Queue<Type> pending)
        {
            var builder = new StringBuilder();
            builder.AppendLine(CultureInfo.InvariantCulture, $"export interface {type.Name} {{");

            foreach (var property in WireProperties(type))
            {
                var (tsType, optional) = MapType(property.PropertyType, pending);
                var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                builder.AppendLine(CultureInfo.InvariantCulture, $"  {name}{(optional ? "?" : "")}: {tsType};");
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 會上 wire 的屬性：公開、可讀寫，且未被 <c>[JsonIgnore]</c> 排除。
        /// </summary>
        private static IEnumerable<PropertyInfo> WireProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .OrderBy(p => p.Name, StringComparer.Ordinal);
        }

        /// <summary>
        /// 把 CLR 型別對映到它在 wire 上的形狀。
        /// </summary>
        /// <returns>TypeScript 型別，以及該成員是否為選填。</returns>
        private static (string TsType, bool Optional) MapType(Type type, Queue<Type> pending)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            var optional = underlying != null || !type.IsValueType;
            var actual = underlying ?? type;

            return (MapNonNullable(actual, pending), optional);
        }

        private static string MapNonNullable(Type type, Queue<Type> pending)
        {
            if (type == typeof(string) || type == typeof(Guid)) return "string";
            if (type == typeof(bool)) return "boolean";
            // A DateTime is an ISO 8601 string on the wire; keeping it as `string` says so.
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "string";
            if (type == typeof(TimeSpan) || type == typeof(DateOnly)) return "string";
            if (type == typeof(byte[])) return "string"; // base64
            if (type == typeof(object)) return "WireValue";
            if (type == typeof(DataSet)) return "DataSet";
            if (type == typeof(DataTable)) return "DataTable";

            if (type.IsPrimitive || type == typeof(decimal)) return "number";

            if (type.IsEnum)
            {
                pending.Enqueue(type);
                return type.Name;
            }

            if (type.IsArray)
            {
                var element = type.GetElementType()!;
                return $"{MapNonNullable(element, pending)}[]";
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                var element = ElementTypeOf(type);
                return element == null ? "unknown[]" : $"{MapNonNullable(element, pending)}[]";
            }

            if (type.IsClass && type.Namespace?.StartsWith("Bee.", StringComparison.Ordinal) == true)
            {
                pending.Enqueue(type);
                return type.Name;
            }

            return "unknown";
        }

        /// <summary>
        /// 取集合的元素型別：泛型參數優先，其次是 <c>KeyCollectionBase</c> 這類自訂集合的基底參數。
        /// </summary>
        private static Type? ElementTypeOf(Type type)
        {
            if (type.IsGenericType) return type.GetGenericArguments().FirstOrDefault();

            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                if (baseType.IsGenericType) return baseType.GetGenericArguments().FirstOrDefault();
            }

            return null;
        }
    }
}
