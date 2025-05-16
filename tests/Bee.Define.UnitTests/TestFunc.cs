using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Define.UnitTests
{
    /// <summary>
    /// 測試用的自訂方法。
    /// </summary>
    internal static class TestFunc
    {
        /// <summary>
        /// 測試物件是否能正常進行 MessagePack 序列化與反序列化，並比對序列化前後有標記 [Key] 的屬性值是否一致。
        /// </summary>
        /// <typeparam name="T">要測試的物件型別。</typeparam>
        /// <param name="obj">要測試的物件實例。</param>
        public static void TestMessagePackSerialization<T>(T obj)
        {
            // 序列化物件
            var serialized = MessagePackHelper.Serialize(obj);

            // 反序列化物件
            var deserialized = MessagePackHelper.Deserialize<T>(serialized);

            // 確認反序列化後的物件不為 null
            Assert.NotNull(deserialized);

            // 比對有 [Key] 標記的屬性值
            foreach (var property in typeof(T).GetProperties())
            {
                // 檢查屬性是否有 [Key] 標記
                var keyAttribute = property.GetCustomAttributes(typeof(MessagePack.KeyAttribute), inherit: true).FirstOrDefault();
                if (keyAttribute != null && property.CanRead)
                {
                    var originalValue = property.GetValue(obj);
                    var deserializedValue = property.GetValue(deserialized);

                    // 驗證序列化前後的屬性值是否一致
                    Assert.Equal(originalValue, deserializedValue);
                }
            }
        }


    }
}
