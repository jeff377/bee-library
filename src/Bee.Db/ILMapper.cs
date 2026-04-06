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
    /// Provides IL-based mapping functionality from <see cref="DbDataReader"/> to type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    public static class ILMapper<T>
    {
        private static readonly ConcurrentDictionary<(Type, string), Delegate> _cache =
            new ConcurrentDictionary<(Type, string), Delegate>();

        /// <summary>
        /// Creates a mapping function that converts a <see cref="DbDataReader"/> row to type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="reader">The DbDataReader containing the query results.</param>
        public static Func<DbDataReader, T> CreateMapFunc(DbDataReader reader)
        {
            // Identify fields present in both the DbDataReader and type T, returning a dictionary of property names to column indexes
            var fieldIndexes = GetMatchingFieldIndexes(reader);
            // Build a composite cache key from the type T and the field index mapping
            var key = (typeof(T), string.Join(",", fieldIndexes.Select(kv => $"{kv.Key}:{kv.Value}")));
            // Return the cached mapper if it already exists
            if (_cache.TryGetValue(key, out var cachedDelegate))
            {
                return (Func<DbDataReader, T>)cachedDelegate;
            }
            // Build the mapper, store it in the cache, and return it
            var mapper = CreateMapper(fieldIndexes);
            _cache[key] = mapper;
            return mapper;
        }

        /// <summary>
        /// Maps all rows from a <see cref="DbDataReader"/> to a <see cref="List{T}"/> using the specified mapper function.
        /// </summary>
        /// <param name="mapper">The mapping function.</param>
        /// <param name="reader">The DbDataReader containing the query results.</param>
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
        /// Maps all rows from a <see cref="DbDataReader"/> to an <see cref="IEnumerable{T}"/> using the specified mapper function.
        /// </summary>
        /// <param name="mapper">The mapping function.</param>
        /// <param name="reader">The DbDataReader containing the query results.</param>
        public static IEnumerable<T> MapToEnumerable(DbDataReader reader, Func<DbDataReader, T> mapper)
        {
            while (reader.Read())
            {
                yield return mapper(reader);
            }
        }

        /// <summary>
        /// Creates a mapping function that converts a <see cref="DbDataReader"/> row to type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="fieldIndexes">A dictionary mapping property names to column indexes.</param>
        /// <returns>A <see cref="Func{DbDataReader, T}"/> delegate.</returns>
        /// <exception cref="InvalidOperationException">Thrown when type <typeparamref name="T"/> has no parameterless constructor.</exception>
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
                var dbReaderMethod = GetDbReaderMethod(fieldType); // Get the best-matching GetXXX() method

                if (dbReaderMethod != null)
                {
                    var endIfLabel = il.DefineLabel();

                    // Check for DBNull: if (!reader.IsDBNull(fieldIndex))
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
        /// Returns the best-matching <c>DbDataReader.GetXXX(int index)</c> method for the given <paramref name="fieldType"/>.
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

            return readerType.GetMethod("GetValue", new[] { typeof(int) }); // Fall back to GetValue for other types
        }

        /// <summary>
        /// Returns a dictionary of property names to column indexes for fields that exist in both the <see cref="DbDataReader"/> and type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="reader">The DbDataReader containing the query results.</param>
        /// <returns>A dictionary mapping property names to their corresponding column indexes.</returns>
        private static Dictionary<string, int> GetMatchingFieldIndexes(DbDataReader reader)
        {
            var fieldIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // Case-insensitive comparison
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);  // Get all writable public properties of T

            // Build a dictionary of DbDataReader column names to their ordinal indexes
            var readerFields = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                readerFields[reader.GetName(i)] = i;
            }

            // Keep only the intersection of T's property names and the DbDataReader column names
            foreach (var prop in properties)
            {
                if (prop.CanWrite && readerFields.TryGetValue(prop.Name, out int index))
                {
                    fieldIndexes[prop.Name] = index;
                }
            }

            return fieldIndexes;
        }

    }
}
