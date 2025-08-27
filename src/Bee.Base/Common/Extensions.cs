using System;
using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// 擴充方法。
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 物件序列化為 XML 字串。
        /// </summary>
        /// <param name="value">物件。</param>
        public static string ToXml(this IObjectSerializeBase value)
        {
            return SerializeFunc.ObjectToXml(value);
        }

        /// <summary>
        /// 將物件序列化為 JSON 字串。
        /// </summary>
        /// <param name="value">物件。</param>
        public static string ToJson(this IObjectSerializeBase value)
        {
            return SerializeFunc.ObjectToJson(value);
        }

        /// <summary>
        /// 將物件序列化為二進位資料。
        /// </summary>
        /// <param name="value">物件。</param>
        public static byte[] ToBinary(this IObjectSerializeBase value)
        {
            return SerializeFunc.ObjectToBinary(value);
        }

        /// <summary>
        /// 將物件序列化為 XML 檔案。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="filePath">XML 檔案路徑。</param>
        public static void ToXmlFile(this IObjectSerializeFile value, string filePath)
        {
            SerializeFunc.ObjectToXmlFile(value, filePath);
        }

        /// <summary>
        /// 將物件序列化為 JSON 檔案。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="filePath">XML 檔案路徑。</param>
        public static void ToJsonFile(this IObjectSerializeFile value, string filePath)
        {
            SerializeFunc.ObjectToJsonFile(value, filePath);
        }

        /// <summary>
        /// 將物件序列化存成檔案
        /// </summary>
        /// <param name="value">物件。</param>
        public static void Save(this IObjectSerializeFile value)
        {
            string sExtension;

            if (StrFunc.IsEmpty(value.ObjectFilePath))
                throw new ArgumentException("ObjectFilePath is empty");

            sExtension = FileFunc.GetExtension(value.ObjectFilePath);
            if (StrFunc.IsEquals(sExtension, ".xml"))
                SerializeFunc.ObjectToXmlFile(value, value.ObjectFilePath);
            else if (StrFunc.IsEquals(sExtension, ".json"))
                SerializeFunc.ObjectToJsonFile(value, value.ObjectFilePath);
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// 取得屬性值。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="collection">屬性集合。</param>
        /// <param name="key">屬性健值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static T GetValue<T>(this PropertyCollection collection, string key, object defaultValue)
        {
            if (collection.Contains(key))
                return (T)collection[key];
            else
                return (T)defaultValue;
        }
    }
}
