using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Bee.Db
{
    /// <summary>
    /// 提供從 <see cref="DbDataReader"/> 到類型 <typeparamref name="T"/> 的映射功能。
    /// </summary>
    /// <typeparam name="T">目標類型。</typeparam>
    public static class ILMapper<T>
    {
        private static readonly ConcurrentDictionary<(Type, string), Delegate> _cache =
            new ConcurrentDictionary<(Type, string), Delegate>();

        /// <summary>
        /// 建立一個對應的映射函式，可用於 DbDataReader 轉換為 T 類型。
        /// </summary>
        /// <param name="reader">資料庫查詢結果的 DbDataReader。</param>
        public static Func<DbDataReader, T> CreateMapFunc(DbDataReader reader)
        {
            // 找出 IDataReader 與 T 類別皆存在的欄位與屬性，傳回包含屬性名稱與對應欄位索引的字典
            var fieldIndexes = DbFunc.GetMatchingFieldIndexes<T>(reader);
            // 取得 T 類型與欄位索引的複合鍵值
            var key = (typeof(T), string.Join(",", fieldIndexes.Select(kv => $"{kv.Key}:{kv.Value}")));
            // 若映射函式已快取，則直接回傳
            if (_cache.TryGetValue(key, out var cachedDelegate))
            {
                return (Func<DbDataReader, T>)cachedDelegate;
            }
            // 建立映射函式加入快取，並回傳映射函式
            var mapper = CreateMapper(fieldIndexes);
            _cache[key] = mapper;
            return mapper;
        }

        /// <summary>
        /// 使用指定映射函式轉換 DbDataReader 為 List。
        /// </summary>
        /// <param name="mapper">映射函式。</param>
        /// <param name="reader">資料庫查詢結果的 DbDataReader。</param>
        public static List<T> MapToList(DbDataReader reader, Func<DbDataReader, T> mapper)
        {
            var list = new List<T>();
            while (reader.Read())
            {
                list.Add(mapper(reader));
            }
            return list;
        }

        /// <summary>
        /// 使用指定的映射函式轉換 DbDataReader 為 IEnumerable。
        /// </summary>
        /// <param name="mapper">映射函式。</param>
        /// <param name="reader">資料庫查詢結果的 DbDataReader。</param>
        public static IEnumerable<T> MapToEnumerable(DbDataReader reader, Func<DbDataReader, T> mapper)
        {
            while (reader.Read())
            {
                yield return mapper(reader);
            }
        }

        /// <summary>
        /// 建立一個對應的映射函式，可用於 DbDataReader 轉換為 T 類型。
        /// </summary>
        /// <param name="fieldIndexes">欄位名稱與索引的映射表</param>
        /// <returns>轉換函式 Func&lt;DbDataReader, T&gt;</returns>
        /// <exception cref="InvalidOperationException">如果 T 類型沒有無參建構子，則拋出異常</exception>
        private static Func<DbDataReader, T> CreateMapper(Dictionary<string, int> fieldIndexes)
        {
            var type = typeof(T);
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                throw new InvalidOperationException($"Type {type.FullName} must have a parameterless constructor.");
            }

            var isDBNullMethod = typeof(DbDataReader).GetMethod("IsDBNull", new[] { typeof(int) });
            var method = new DynamicMethod("MapReaderToEntity", type, new[] { typeof(DbDataReader) }, type, true);
            var il = method.GetILGenerator();
            var result = il.DeclareLocal(type);

            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc, result);

            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                var setMethod = prop.GetSetMethod();
                if (setMethod == null || !fieldIndexes.TryGetValue(prop.Name, out int fieldIndex))
                {
                    continue;
                }

                var fieldType = prop.PropertyType;
                var dbReaderMethod = GetDbReaderMethod(fieldType); // 取得最佳 `GetXXX()` 方法

                if (dbReaderMethod != null)
                {
                    var endIfLabel = il.DefineLabel();

                    // `if (!reader.IsDBNull(fieldIndex))` 檢查是否為 `DBNull`
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, fieldIndex);
                    il.Emit(OpCodes.Callvirt, isDBNullMethod);
                    il.Emit(OpCodes.Brtrue, endIfLabel);

                    // `reader.GetXXX(fieldIndex)`
                    il.Emit(OpCodes.Ldloc, result);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, fieldIndex);
                    il.Emit(OpCodes.Callvirt, dbReaderMethod);
                    il.Emit(OpCodes.Callvirt, setMethod);

                    il.MarkLabel(endIfLabel);
                }
            }

            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);

            return (Func<DbDataReader, T>)method.CreateDelegate(typeof(Func<DbDataReader, T>));
        }

        /// <summary>
        /// 根據 `PropertyType` 取得最佳 `DbDataReader.GetXXX(int index)` 方法。
        /// </summary>
        private static MethodInfo GetDbReaderMethod(Type fieldType)
        {
            var readerType = typeof(DbDataReader);

            if (fieldType == typeof(int)) return readerType.GetMethod("GetInt32", new[] { typeof(int) });
            if (fieldType == typeof(string)) return readerType.GetMethod("GetString", new[] { typeof(int) });
            if (fieldType == typeof(bool)) return readerType.GetMethod("GetBoolean", new[] { typeof(int) });
            if (fieldType == typeof(DateTime)) return readerType.GetMethod("GetDateTime", new[] { typeof(int) });
            if (fieldType == typeof(decimal)) return readerType.GetMethod("GetDecimal", new[] { typeof(int) });
            if (fieldType == typeof(double)) return readerType.GetMethod("GetDouble", new[] { typeof(int) });
            if (fieldType == typeof(float)) return readerType.GetMethod("GetFloat", new[] { typeof(int) });
            if (fieldType == typeof(long)) return readerType.GetMethod("GetInt64", new[] { typeof(int) });
            if (fieldType == typeof(short)) return readerType.GetMethod("GetInt16", new[] { typeof(int) });

            return readerType.GetMethod("GetValue", new[] { typeof(int) }); // 其他類型仍使用 `GetValue`
        }



    }
}
