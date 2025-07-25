﻿using System;
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
                    new DataTableFormatter(), // 自訂 DataTable 格式化器
                    new DataSetFormatter()    // 自訂 DataSet 格式化器
                },
                new IFormatterResolver[]
                {
                    TypelessContractlessStandardResolver.Instance, // 加入支援 object 多型別
                    FormatterResolver.Instance,   // 自訂解析器
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
            return MessagePackSerializer.Serialize(value, Options);
        }

        /// <summary>
        /// 序列化物件為 byte[]。
        /// </summary>
        /// <param name="value">待序列化的物件。</param>
        /// <param name="type">物件的型別。</param>
        /// <returns>序列化後的 byte[]。</returns>
        public static byte[] Serialize(object value, Type type)
        {
            return MessagePackSerializer.Serialize(type, value, Options);
        }

        /// <summary>
        /// 反序列化 byte[] 為物件。
        /// </summary>
        /// <typeparam name="T">反序列化後的物件類型。</typeparam>
        /// <param name="data">要反序列化的 byte[]。</param>
        /// <returns>反序列化後的物件。</returns>
        public static T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, Options);
        }

        /// <summary>
        /// 反序列化 byte[] 為物件。
        /// </summary>
        /// <param name="data">要反序列化的 byte[]。</param>
        /// <param name="type">物件型別。</param>
        /// <returns>反序列化後的物件。</returns>
        public static object Deserialize(byte[] data, Type type)
        {
            return MessagePackSerializer.Deserialize(type, data, Options);  
        }
    }

}
