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
        public static EFieldDbType ToFieldDbType(Type type)
        {
            TypeCode oTypeCode;

            oTypeCode = ToTypeCode(type);
            switch (oTypeCode)
            {
                case TypeCode.Boolean:
                    return EFieldDbType.Boolean;
                case TypeCode.Char:
                case TypeCode.String:
                    return EFieldDbType.String;
                case TypeCode.DateTime:
                    return EFieldDbType.DateTime;
                case TypeCode.Decimal:
                    return EFieldDbType.Currency;
                case TypeCode.Double:
                case TypeCode.Single:
                    return EFieldDbType.Double;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return EFieldDbType.Integer;
                default:
                    throw new TException("{0} can't convert to FieldDbType", type.Name);
            }
        }

        /// <summary>
        /// 將 EFieldDbType 轉型為 DbType 型別。
        /// </summary>
        /// <param name="fieldDbType">欄位資料型別。</param>
        public static DbType ToDbType(EFieldDbType fieldDbType)
        {
            switch (fieldDbType)
            {
                case EFieldDbType.String:
                    return DbType.String;
                case EFieldDbType.Text:
                    return DbType.String;
                case EFieldDbType.Boolean:
                    return DbType.Boolean;
                case EFieldDbType.Integer:
                    return DbType.Int32;
                case EFieldDbType.Double:
                    return DbType.Double;
                case EFieldDbType.Currency:
                    return DbType.Currency;
                case EFieldDbType.Date:
                    return DbType.Date;
                case EFieldDbType.DateTime:
                    return DbType.DateTime;
                case EFieldDbType.Guid:
                    return DbType.Guid;
                case EFieldDbType.Binary:
                    return DbType.Binary;
                default:
                    throw new TException("{0} can't convert to DbType", fieldDbType);
            }
        }

        /// <summary>
        /// 將 EFieldDbType 轉型為 System.Type。
        /// </summary>
        /// <param name="fieldDbType">欄位資料型別。</param>
        public static Type ToType(EFieldDbType fieldDbType)
        {
            switch (fieldDbType)
            {
                case EFieldDbType.String:
                    return typeof(string);
                case EFieldDbType.Text:
                    return typeof(string);
                case EFieldDbType.Boolean:
                    return typeof(bool);
                case EFieldDbType.Integer:
                    return typeof(int);
                case EFieldDbType.Double:
                    return typeof(double);
                case EFieldDbType.Currency:
                    return typeof(decimal);
                case EFieldDbType.Date:
                case EFieldDbType.DateTime:
                    return typeof(DateTime);
                case EFieldDbType.Guid:
                    return typeof(Guid);
                case EFieldDbType.Binary:
                    return typeof(byte[]);
                default:
                    throw new TException("{0} can't convert to System.Type", fieldDbType);
            }
        }
    }
}
