using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Define
{
    /// <summary>
    /// 提供 MessagePack 序列化與反序列化的靜態方法，並使用自訂的格式化器與解析器。
    /// </summary>
    public static class MessagePackHelper
    {
        /// <summary>
        /// 靜態初始化 MessagePack 序列化選項，包含自訂格式化器與解析器。
        /// </summary>
        private static readonly MessagePackSerializerOptions Options;

        /// <summary>
        /// 靜態建構函式，初始化 MessagePack 序列化選項。
        /// </summary>
        static MessagePackHelper()
        {
            // 建立自訂的格式化器與解析器
            var resolver = CompositeResolver.Create(
                new IMessagePackFormatter[]
                {
                    new TDataTableFormatter(), // 自訂 DataTable 格式化器
                    new TDataSetFormatter()    // 自訂 DataSet 格式化器
                },
                new IFormatterResolver[]
                {
                    TypelessContractlessStandardResolver.Instance, // 加入支援 object 多型別
                    TFormatterResolver.Instance,   // 自訂解析器
                    StandardResolver.Instance      // 標準解析器
                });

            // 設定 MessagePack 序列化選項
            Options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        /// <summary>
        /// 序列化物件為 byte[]。
        /// </summary>
        /// <typeparam name="T">序列化的物件類型。</typeparam>
        /// <param name="value">待序列化的物件。</param>
        /// <returns>序列化後的 byte[]。</returns>
        public static byte[] Serialize<T>(T value)
        {
            // 使用靜態的 MessagePackSerializerOptions 進行序列化
            return MessagePackSerializer.Serialize(value, Options);
        }

        /// <summary>
        /// 反序列化 byte[] 為物件。
        /// </summary>
        /// <typeparam name="T">反序列化後的物件類型。</typeparam>
        /// <param name="data">要反序列化的 byte[]。</param>
        /// <returns>反序列化後的物件。</returns>
        public static T Deserialize<T>(byte[] data)
        {
            // 使用靜態的 MessagePackSerializerOptions 進行反序列化
            return MessagePackSerializer.Deserialize<T>(data, Options);
        }
    }

}
