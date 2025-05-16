using System.Reflection;
using Bee.Base;

namespace Bee.Define.UnitTests
{
    /// <summary>
    /// 測試用的自訂方法。
    /// </summary>
    internal static class TestFunc
    {
        /// <summary>
        /// 測試 MessagePack 的序列化與反序列化。
        /// </summary>
        public static void TestMessagePackSerialization<T>(T obj)
        {
            // 序列化與反序列化
            var serialized = MessagePackHelper.Serialize(obj);
            var deserialized = MessagePackHelper.Deserialize<T>(serialized);

            Assert.NotNull(deserialized);

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 檢查屬性是否有 [Key] 標記
                var keyAttribute = property.GetCustomAttributes(typeof(MessagePack.KeyAttribute), inherit: true).FirstOrDefault();
                if (keyAttribute != null && property.CanRead)
                {
                    var originalValue = property.GetValue(obj);
                    var deserializedValue = property.GetValue(deserialized);

                    if (originalValue == null && deserializedValue == null)
                    {
                        continue; // 都是 null，跳過
                    }

                    // 簡單型別直接比較
                    if (IsSimpleType(property.PropertyType))
                    {
                        Assert.Equal(originalValue, deserializedValue);
                    }
                    else if (originalValue is IEnumerable<TParameter> origList && deserializedValue is IEnumerable<TParameter> deserList)
                    {
                        // 特別處理 TParameterCollection 的內容比對
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
                        // 其他複雜型別（不支援深層遞迴比對），轉為 JSON 比較
                        var json1 = SerializeFunc.ObjectToXml(originalValue);
                        var json2 = SerializeFunc.ObjectToXml(deserializedValue);
                        Assert.Equal(json1, json2);
                    }
                }
            }
        }

        /// <summary>
        /// 判斷是否為簡單型別。
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type.Equals(typeof(string))
                || type.Equals(typeof(DateTime))
                || type.Equals(typeof(decimal))
                || type.Equals(typeof(Guid));
        }

    }
}
