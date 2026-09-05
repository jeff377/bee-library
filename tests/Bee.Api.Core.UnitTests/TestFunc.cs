using Bee.Api.Core.MessagePack;
using Bee.Definition.Collections;
using System.Reflection;
using Bee.Base.Serialization;
using MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 測試用的自訂方法。
    /// </summary>
    internal static class TestFunc
    {
        /// <summary>
        /// 測試 MessagePack 的序列化與反序列化，並逐一比對還原後的屬性值。
        /// </summary>
        /// <remarks>
        /// 比對範圍為「所有 public 可讀且未標記 <see cref="IgnoreMemberAttribute"/> 的屬性」，
        /// 刻意不以 <c>[Key]</c> 是否存在作為閘門 —— adr-030 的 name-based key 遷移後，
        /// 絕大多數型別改用 <c>[MessagePackObject(keyAsPropertyName: true)]</c>，屬性上不再有
        /// <c>[Key]</c>；若以此為閘門，比對迴圈會一次都不執行，整個 helper 會退化成只剩
        /// <c>Assert.NotNull</c> 的假綠燈。
        /// </remarks>
        public static void TestMessagePackSerialization<T>(T obj)
        {
            // 序列化與反序列化
            var serialized = MessagePackCodec.Serialize(obj);
            var deserialized = MessagePackCodec.Deserialize<T>(serialized);

            Assert.NotNull(deserialized);

            var comparedCount = 0;
            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead) { continue; }

                // 索引子無法直接取值
                if (property.GetIndexParameters().Length > 0) { continue; }

                // 標記為不序列化的成員本來就不會被還原，比對它們只會製造假紅燈
                if (property.IsDefined(typeof(IgnoreMemberAttribute), inherit: true)) { continue; }

                var originalValue = property.GetValue(obj);
                var deserializedValue = property.GetValue(deserialized);
                comparedCount++;

                if (originalValue == null && deserializedValue == null)
                {
                    continue; // 都是 null，跳過
                }

                // 簡單型別直接比較
                if (IsSimpleType(property.PropertyType))
                {
                    Assert.Equal(originalValue, deserializedValue);
                }
                else if (originalValue is IEnumerable<Parameter> origList && deserializedValue is IEnumerable<Parameter> deserList)
                {
                    // 特別處理 ParameterCollection 的內容比對
                    Assert.Equal(origList.Count(), deserList.Count());

                    foreach (var origItem in origList)
                    {
                        var match = deserList.FirstOrDefault(x => x.Key == origItem.Key);
                        Assert.NotNull(match);
                        Assert.Equal(origItem.Value, match.Value);
                    }
                }
                else
                {
                    // 其他複雜型別（不支援深層遞迴比對），序列化為 XML 後比對
                    Assert.NotNull(originalValue);
                    Assert.NotNull(deserializedValue);
                    var xml1 = XmlCodec.Serialize(originalValue);
                    var xml2 = XmlCodec.Serialize(deserializedValue);
                    Assert.Equal(xml1, xml2);
                }
            }

            // 防止本 helper 再次靜默退化：一個屬性都沒比對到，代表閘門條件出了問題
            Assert.True(comparedCount > 0,
                $"型別 {typeof(T).Name} 沒有任何屬性被比對，helper 可能已失效。");
        }

        /// <summary>
        /// 判斷是否為簡單型別。
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            var actual = Nullable.GetUnderlyingType(type) ?? type;

            return actual.IsPrimitive
                || actual.IsEnum
                || actual == typeof(string)
                || actual == typeof(DateTime)
                || actual == typeof(DateTimeOffset)
                || actual == typeof(DateOnly)
                || actual == typeof(TimeOnly)
                || actual == typeof(TimeSpan)
                || actual == typeof(decimal)
                || actual == typeof(Guid);
        }
    }
}
