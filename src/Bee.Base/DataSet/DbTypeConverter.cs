﻿using System;
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
        public static TypeCode ToTypeCode(this Type type)
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
            TypeCode oTypeCode;

            oTypeCode = ToTypeCode(type);
            switch (oTypeCode)
            {
                case TypeCode.Boolean:
                    return FieldDbType.Boolean;
                case TypeCode.Char:
                case TypeCode.String:
                    return FieldDbType.String;
                case TypeCode.DateTime:
                    return FieldDbType.DateTime;
                case TypeCode.Decimal:
                    return FieldDbType.Currency;
                case TypeCode.Double:
                case TypeCode.Single:
                    return FieldDbType.Double;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return FieldDbType.Integer;
                default:
                    throw new InvalidOperationException($"{type.Name} can't convert to FieldDbType");
            }
        }

        /// <summary>
        /// 將 EFieldDbType 轉型為 DbType 型別。
        /// </summary>
        /// <param name="fieldDbType">欄位資料型別。</param>
        public static DbType ToDbType(FieldDbType fieldDbType)
        {
            switch (fieldDbType)
            {
                case FieldDbType.String:
                    return DbType.String;
                case FieldDbType.Text:
                    return DbType.String;
                case FieldDbType.Boolean:
                    return DbType.Boolean;
                case FieldDbType.Integer:
                    return DbType.Int32;
                case FieldDbType.Double:
                    return DbType.Double;
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
                    throw new InvalidOperationException($"{fieldDbType} can't convert to DbType");
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
                case FieldDbType.Integer:
                    return typeof(int);
                case FieldDbType.Double:
                    return typeof(double);
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
