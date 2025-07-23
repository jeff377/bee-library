using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;

namespace Bee.Base
{
    /// <summary>
    /// 基本函式庫。
    /// </summary>
    public static class BaseFunc
    {
        /// <summary>
        /// 是否為 DBNull 值。
        /// </summary>
        /// <param name="value">要判斷的值。</param>
        public static bool IsDBNull(object value)
        {
            return Convert.IsDBNull(value);
        }

        /// <summary>
        /// 是否為 null 或 DBNull 值。
        /// </summary>
        /// <param name="value">要判斷的值。</param>
        public static bool IsNullOrDBNull(object value)
        {
            return value == null || Convert.IsDBNull(value);
        }

        /// <summary>
        /// 判斷指定的位元組陣列是否為 null 或長度為 0。
        /// </summary>
        /// <param name="bytes">要檢查的位元組陣列。</param>
        /// <returns>若為 null 或長度為 0，則傳回 true；否則傳回 false。</returns>
        public static bool IsNullOrEmpty(byte[] bytes)
        {
            return bytes == null || bytes.Length == 0;
        }

        /// <summary>
        /// 是否為空值，Null 或 DBNull 皆視為空值。
        /// </summary>
        /// <param name="value">要判斷的值。</param>
        public static bool IsEmpty(object value)
        {
            switch (value)
            {
                case null:
                    return true;
                case DateTime dateTimeValue:
                    return IsEmpty(dateTimeValue);
                case string stringValue:
                    return IsEmpty(stringValue);
                case Guid guidValue:
                    return IsEmpty(guidValue);
                case IList listValue:
                    return IsEmpty(listValue);
                default:
                    return IsNullOrDBNull(value);
            }
        }

        /// <summary>
        /// 判斷字串是否為空字串。
        /// </summary>
        /// <param name="value">要判斷的字串。</param>
        public static bool IsEmpty(string value)
        {
            return StrFunc.IsEmpty(value, true);
        }

        /// <summary>
        /// 判斷 Guid 值是否為空值。
        /// </summary>
        /// <param name="value">要判斷的 Guid 值。</param>
        public static bool IsEmpty(Guid value)
        {
            return (value == Guid.Empty);
        }

        /// <summary>
        /// 判斷日期值是否為空值。
        /// </summary>
        /// <param name="value">要判斷的日期值。</param>
        public static bool IsEmpty(DateTime value)
        {
            // SQL 資料庫的 DateTime 最小值為 1753/1/1，小於此值視為空值
            // 日期為 Null、DbNull、DateTime.MinValue 皆視為空值
            return (IsNullOrDBNull(value) || value < new DateTime(1753, 1, 1));
        }

        /// <summary>
        /// 判斷 IList 型別的集合是否無資料。
        /// </summary>
        /// <param name="value">要判斷的集合。</param>
        public static bool IsEmpty(IList value)
        {
            if (value != null && value.Count != 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 判斷 IEnumerable 型別的集合是否無資料。
        /// </summary>
        /// <param name="enumerable">要判斷的集合。</param>
        public static bool IsEmpty(IEnumerable enumerable)
        {
            if (enumerable == null) return true;

            var enumerator = enumerable.GetEnumerator();
            return !enumerator.MoveNext(); // 判斷是否有至少一筆資料
        }

        /// <summary>
        /// 檢查指定的位元組陣列是否為空（null 或長度為 0）。
        /// </summary>
        /// <param name="data">位元組陣列。</param>
        /// <returns>若為 null 或空陣列則回傳 true，否則為 false。</returns>
        public static bool IsEmpty(byte[] data)
        {
            return data == null || data.Length == 0;
        }

        /// <summary>
        /// 判斷資料表是否無資料。
        /// </summary>
        /// <param name="table">要判斷的資料表。</param>
        public static bool IsEmpty(DataTable table)
        {
            return DataSetFunc.IsEmpty(table);
        }

        /// <summary>
        /// 取得列舉成員的名稱。
        /// </summary>
        /// <param name="value">列舉值。</param>
        public static string GetEnumName(Enum value)
        {
            return Enum.GetName(value.GetType(), value);
        }

        /// <summary>
        /// 轉型為文字。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">無法成功轉型的預設值。</param>
        public static string CStr(object value, string defaultValue)
        {
            // 若為 null  或 DBNull 值，則傳回預設值
            if (BaseFunc.IsNullOrDBNull(value))
                return defaultValue;
            // 若為列舉型別，則傳回列舉名稱
            if (value is Enum e)
                return GetEnumName(e);
            // 轉型為文字
            return value.ToString();
        }

        /// <summary>
        /// 轉型為文字。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        public static string CStr(object value)
        {
            return BaseFunc.CStr(value, string.Empty);
        }

        /// <summary>
        /// 轉型為布林值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static bool CBool(string value, bool defaultValue = false)
        {
            if (StrFunc.IsEmpty(value))
                return defaultValue;
            if (StrFunc.IsEqualsOr(value, "1", "T", "TRUE", "Y", "YES", "是", "真"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// 轉型為布林值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static bool CBool(object value, bool defaultValue = false)
        {
            return value is bool ? (bool)value : CBool(CStr(value), defaultValue);
        }

        /// <summary>
        /// 轉型為列舉值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="type">列舉型別。</param>
        public static object CEnum(string value, Type type)
        {
            return Enum.Parse(type, value, true);
        }

        /// <summary>
        /// 轉型為列舉值。
        /// </summary>
        /// <typeparam name="T">列舉型別。</typeparam>
        /// <param name="value">要轉型的值。</param>
        public static T CEnum<T>(string value)
        {
            return (T)CEnum(value, typeof(T));
        }

        /// <summary>
        /// 判斷指定的物件是否可被視為數值（支援 string、bool、enum）。
        /// </summary>
        /// <param name="value">要判斷的值。</param>
        /// <returns>若能轉型為數值則回傳 true。</returns>
        public static bool IsNumeric(object value)
        {
            if (value == null)
                return false;

            // bool 與 enum 視為可轉為數值
            if (value is bool || value is Enum)
                return true;

            // 針對 string 做特殊處理
            var s = value as string;
            if (s != null)
                return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _);

            // 基本數值型別
            if (value is byte || value is sbyte ||
                value is short || value is ushort ||
                value is int || value is uint ||
                value is long || value is ulong ||
                value is float || value is double || value is decimal)
                return true;

            // 最後一線防守：ToString 後再判斷（防止反射動態物件等）
            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }


        /// <summary>
        /// 判斷字串是否為指定長度的數值。
        /// </summary>
        /// <param name="value">要判斷的值。</param>
        /// <param name="length">長度。</param>
        public static bool IsNumeric(string value, int length)
        {
            if (BaseFunc.IsNumeric(value) && StrFunc.Length(value) == length)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 將傳入的物件轉換為數值型別。支援 string、bool、enum 與各種數值型別。
        /// 若無法轉換，將拋出 InvalidCastException。
        /// </summary>
        /// <param name="value">要轉型的值，可為 string、bool、enum 或數值型別。</param>
        /// <returns>轉換後的數值。string 會轉為 double，bool 轉為 1 或 0，enum 轉為整數。</returns>
        /// <exception cref="InvalidCastException">若無法轉換為數值，則拋出此例外。</exception>
        public static object ConvertToNumber(object value)
        {
            if (IsNullOrDBNull(value))
                return 0;

            var s = value as string;
            if (s != null)
            {
                if (IsEmpty(s))
                    return 0;

                double result;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    return result;
            }

            if (value is bool)
                return (bool)value ? 1 : 0;

            if (value is Enum)
                return Convert.ToInt32(value);

            if (value is byte || value is sbyte ||
                value is short || value is ushort ||
                value is int || value is uint ||
                value is long || value is ulong ||
                value is float || value is double || value is decimal)
                return value;

            double fallback;
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out fallback))
                return fallback;

            throw new InvalidCastException($"Cannot convert '{value}' to number.");
        }

        /// <summary>
        /// 轉型為整數，若無法轉換則傳回預設值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static int CInt(object value, int defaultValue = 0)
        {
            if (BaseFunc.IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                if (value is Enum)
                    return Convert.ToInt32(value);
                else
                    return Convert.ToInt32(ConvertToNumber(value));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 轉型為 double 型別浮點數，若無法轉換則傳回預設值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static double CDouble(object value, double defaultValue = 0)
        {
            if (BaseFunc.IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                return Convert.ToDouble(ConvertToNumber(value == null ? defaultValue : value));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 轉型為 decimal 型別浮點數，精確度高達 28-29 個數字，若無法轉換則傳回預設值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static decimal CDecimal(object value, decimal defaultValue = 0)
        {
            if (BaseFunc.IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                return Convert.ToDecimal(ConvertToNumber(value == null ? defaultValue : value));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 轉型為日期時間值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static DateTime CDateTime(object value, DateTime defaultValue = default)
        {
            string sValue;

            if (IsNullOrDBNull(value)) { return defaultValue; }
            if (StrFunc.IsEmpty(value)) { return defaultValue; }
            if (DateTimeFunc.IsDate(value)) { return Convert.ToDateTime(value); }

            try
            {
                // 轉換為字串，並去除日期分隔符號
                sValue = BaseFunc.CStr(value);
                return StrToDate(sValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 字串轉型為日期。
        /// </summary>
        /// <param name="value">描述日期的字串。</param>
        private static DateTime StrToDate(string value)
        {
            string sValue;
            string sDate;
            int iLen;

            // 去除日期分隔符號
            sValue = value.Replace("/", string.Empty);
            sValue = sValue.Replace("-", string.Empty);
            // 全為數值才是允許轉換日期的格式
            if (!BaseFunc.IsNumeric(sValue)) { return DateTime.MinValue; }
            // 依字串長度，嘗試做日期轉換
            iLen = StrFunc.Length(sValue);
            switch (iLen)
            {
                case 8: // 8碼西元日期，例如 20150312
                    sDate = sValue.Insert(4, "-").Insert(7, "-");
                    break;
                case 7: // 7碼民國日期，例如 1040312
                    sDate = StrFunc.Format("{0}-{1}-{2}", BaseFunc.CInt(StrFunc.Left(sValue, 3)) + 1911,
                        StrFunc.Substring(sValue, 3, 2), StrFunc.Substring(sValue, 5, 2));
                    break;
                case 6: // 6碼西元年月，例如 201503
                    sDate = BaseFunc.CStr(value).Insert(4, "-") + "-01";
                    break;
                case 5: // 5碼民國年月，例如 10403
                    sDate = StrFunc.Format("{0}-{1}-01", BaseFunc.CInt(StrFunc.Left(sValue, 3)) + 1911,
                        StrFunc.Substring(sValue, 3, 2));
                    break;
                case 4: // 4碼西元年，例如 2015
                    sDate = StrFunc.Format("{0}-01-01", sValue);
                    break;
                case 3: // 3碼民國年，例如 104
                    sDate = StrFunc.Format("{0}-01-01", BaseFunc.CInt(sValue) + 1911);
                    break;
                default:
                    sDate = string.Empty;
                    break;
            }

            if (StrFunc.IsNotEmpty(sDate))
                return Convert.ToDateTime(sDate);
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// 轉型為日期值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static DateTime CDate(object value, DateTime defaultValue = default)
        {
            return CDateTime(value, defaultValue).Date;
        }

        /// <summary>
        /// 轉型為 Guid 值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        public static Guid CGuid(string value)
        {
            if (IsEmpty(value))
                return Guid.Empty;
            else
                return new Guid(value);
        }

        /// <summary>
        /// 轉型為 Guid 值。
        /// </summary>
        /// <param name="value">要轉型的值。</param>
        public static Guid CGuid(object value)
        {
            if (IsNullOrDBNull(value))
                return Guid.Empty;
            else if (value is Guid)
                return (Guid)value;
            else if (value is string)
                return CGuid((string)value);
            else
                return Guid.Empty;
        }

        /// <summary>
        /// 轉型為欄位資料型別的值。
        /// </summary>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="value">傳入值。</param>
        public static object CFieldValue(FieldDbType dbType, object value)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return CStr(value);
                case FieldDbType.Boolean:
                    return CBool(value);
                case FieldDbType.Integer:
                    return CInt(value);
                case FieldDbType.Double:
                    return CDouble(value);
                case FieldDbType.Currency:
                    return CDecimal(value);
                case FieldDbType.Date:
                    return CDate(value);
                case FieldDbType.DateTime:
                    return CDateTime(value);
                case FieldDbType.Guid:
                    return CGuid(value);
                default:
                    return value;
            }
        }

        /// <summary>
        /// 轉型為要儲存至資料庫的值。
        /// </summary>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="value">傳入值。</param>
        public static object CDbFieldValue(FieldDbType dbType, object value)
        {
            if (value is DateTime)
            {
                if (BaseFunc.CDateTime(value) == DateTime.MinValue)
                    return DBNull.Value;
            }
            return CFieldValue(dbType, value);
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="objectSerialize">物件序列化介面。</param>
        /// <param name="serializeState">序列化狀態。</param>
        public static void SetSerializeState(IObjectSerialize objectSerialize, SerializeState serializeState)
        {
            objectSerialize?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// 序列化時是否為空值，Null 或 DBNull 皆視為空值。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        /// <param name="value">要判斷的值。</param>
        public static bool IsSerializeEmpty(SerializeState serializeState, object value)
        {
            if (serializeState != SerializeState.Serialize) { return false; }

            switch (value)
            {
                case null:
                    return true;
                case IObjectSerializeEmpty objectSerializeEmpty:
                    return objectSerializeEmpty.IsSerializeEmpty;
                case IList listValue:
                    return IsEmpty(listValue);
                case IEnumerable enumerableValue:
                    return IsEmpty(enumerableValue);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 產生新的 Guid 值。
        /// </summary>
        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// 產生新的 Guid 值字串。
        /// </summary>
        public static string NewGuidString()
        {
            return NewGuid().ToString();
        }

        /// <summary>
        /// 隨機取得一個整數值。
        /// </summary>
        /// <param name="min">最小值。</param>
        /// <param name="max">最大值。</param>
        public static int RndInt(int min, int max)
        {
            Random oRandom;

            oRandom = new Random(Guid.NewGuid().GetHashCode());
            return oRandom.Next(min, max);
        }

        /// <summary>
        /// 建立指定型別的執行個體。
        /// </summary>
        /// <param name="assemblyName">組件名稱。</param>
        /// <param name="typeName">型別名稱。</param>
        /// <param name="args">建構函式引數。</param>
        public static object CreateInstance(string assemblyName, string typeName, params object[] args)
        {
            return AssemblyLoader.CreateInstance(assemblyName, typeName, args);
        }

        /// <summary>
        /// 建立指定型別的執行個體。
        /// </summary>
        /// <param name="typeName">型別名稱，格式為 "Bee.Business.TBusinessObject, Bee.Business" 或 "Bee.Business.TBusinessObject"。</param>
        /// <param name="args">建構函式引數。</param>
        public static object CreateInstance(string typeName, params object[] args)
        {
            return AssemblyLoader.CreateInstance(typeName, args);
        }

        /// <summary>
        /// 取得元件的指定 Attribute。
        /// </summary>
        /// <param name="component">元件。</param>
        /// <param name="attributeType">Attribute 型別。</param>
        public static Attribute GetAttribute(object component, Type attributeType)
        {
            return TypeDescriptor.GetAttributes(component)[attributeType];
        }

        /// <summary>
        /// 取得屬性的指定 Attribute。
        /// </summary>
        /// <param name="component">元件。</param>
        /// <param name="propertyName">屬性名稱。</param>
        /// <param name="attributeType">Attribute 型別。</param>
        public static Attribute GetPropertyAttribute(object component, string propertyName, Type attributeType)
        {
            var property = TypeDescriptor.GetProperties(component)[propertyName];
            return property?.Attributes[attributeType];
        }

        /// <summary>
        /// 取得元件的指定屬性值。
        /// </summary>
        /// <param name="component">元件。</param>
        /// <param name="propertyName">屬性名稱。</param>
        public static object GetPropertyValue(object component, string propertyName)
        {
            var property = TypeDescriptor.GetProperties(component)[propertyName];
            return property?.GetValue(component);
        }

        /// <summary>
        /// 設定元件的指定屬性值。
        /// </summary>
        /// <param name="component">物件。</param>
        /// <param name="propertyName">屬性名稱。</param>
        /// <param name="propertyValue">要寫入的屬性值。</param>
        public static void SetPropertyValue(object component, string propertyName, object propertyValue)
        {
            TypeDescriptor.GetProperties(component)[propertyName].SetValue(component, propertyValue);
        }

        /// <summary>
        /// 判斷傳入值是否為指定的泛型型別。
        /// </summary>
        /// <param name="value">傳入值。</param>
        /// <param name="genericType">泛型型別。</param>
        public static bool IsGenericType(object value, Type genericType)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (genericType == null)
                throw new ArgumentNullException(nameof(genericType));

            Type type = value.GetType();

            // 檢查該型別是否為指定的泛型型別
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                return true;

            // 檢查該型別是否繼承自某個指定的泛型型別
            while ((type = type.BaseType) != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 驗證物件的型別是否符合。
        /// </summary>
        /// <param name="value">要驗證的物件。</param>
        /// <param name="types">要判斷的型別陣列。</param>
        public static bool CheckTypes(object value, params Type[] types)
        {
            foreach (Type type in types)
            {
                if (type.IsInstanceOfType(value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 取得命令列引數。
        /// </summary>
        public static Dictionary<string, string> GetCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < args.Length; i++) // 跳過 args[0]（執行檔名）
            {
                if (!args[i].StartsWith("--")) continue;

                string key = args[i].Substring(2); // 去掉 "--"
                // 若下一個參數 (i + 1) 存在且不是以 "-" 開頭 (代表不是新的選項)，則取該參數作為值，並讓 i 前進一格；
                // 否則，預設值為 "true"（適用於類似 "--flag" 這樣的開關選項）。
                string value = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[++i] : "true";

                result[key] = value;
            }
            return result;
        }
    }
}
