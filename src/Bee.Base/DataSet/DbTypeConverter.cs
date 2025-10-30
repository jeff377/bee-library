using System;
using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// 資料型別轉換。
    /// </summary>
    public static class DbTypeConverter
    {
        /// <summary>
        /// 將指定型別轉換為 TypeCode 列舉型別。
        /// </summary>
        /// <param name="type">型別。</param>
        public static TypeCode ToTypeCode(Type type)
        {
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                type = type.GetGenericArguments()[0];
            else if (type.IsByRef)
                type = type.GetElementType();
            return Type.GetTypeCode(type);
        }

        /// <summary>
        /// 將指定型別轉換為 FieldDbType 列舉型別。
        /// </summary>
        /// <param name="type">型別。</param>
        public static FieldDbType ToFieldDbType(Type type)
        {
            var typeCode = ToTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return FieldDbType.Boolean;
                case TypeCode.Char:
                case TypeCode.String:
                    return FieldDbType.String;
                case TypeCode.DateTime:
                    return FieldDbType.DateTime;
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return FieldDbType.Decimal;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return FieldDbType.Short;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return FieldDbType.Integer;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return FieldDbType.Long;
                default:
                    throw new InvalidOperationException($"{type.Name} can't convert to FieldDbType");
            }
        }

        /// <summary>
        /// 將 FieldDbType 轉型為 DbType 型別。
        /// </summary>
        /// <param name="fieldDbType">欄位資料型別。</param>
        public static DbType ToDbType(FieldDbType fieldDbType)
        {
            switch (fieldDbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return DbType.String;
                case FieldDbType.Boolean:
                    return DbType.Boolean;
                case FieldDbType.AutoIncrement:
                case FieldDbType.Integer:
                    return DbType.Int32;
                case FieldDbType.Short:
                    return DbType.Int16;
                case FieldDbType.Long:
                    return DbType.Int64;
                case FieldDbType.Decimal:
                    return DbType.Decimal;
                case FieldDbType.Currency:
                    return DbType.Currency;
                case FieldDbType.Date:
                    return DbType.Date;
                case FieldDbType.DateTime:
                    return DbType.DateTime;
                case FieldDbType.Guid:
                    return DbType.Guid;
                case FieldDbType.Binary:
                    return DbType.Binary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fieldDbType), $"Unsupported EFieldDbType: {fieldDbType}");
            }
        }

        /// <summary>
        /// 將 EFieldDbType 轉型為 System.Type。
        /// </summary>
        /// <param name="fieldDbType">欄位資料型別。</param>
        public static Type ToType(FieldDbType fieldDbType)
        {
            switch (fieldDbType)
            {
                case FieldDbType.String:
                    return typeof(string);
                case FieldDbType.Text:
                    return typeof(string);
                case FieldDbType.Boolean:
                    return typeof(bool);
                case FieldDbType.AutoIncrement:
                    return typeof(int);
                case FieldDbType.Short:
                    return typeof(short);
                case FieldDbType.Integer:
                    return typeof(int);
                case FieldDbType.Long:
                    return typeof(long);
                case FieldDbType.Decimal:
                    return typeof(decimal);
                case FieldDbType.Currency:
                    return typeof(decimal);
                case FieldDbType.Date:
                case FieldDbType.DateTime:
                    return typeof(DateTime);
                case FieldDbType.Guid:
                    return typeof(Guid);
                case FieldDbType.Binary:
                    return typeof(byte[]);
                default:
                    throw new InvalidOperationException($"{fieldDbType} can't convert to System.Type");
            }
        }
    }
}
