using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bee.Base
{
    /// <summary>
    /// 序列化函式庫。
    /// </summary>
    public static class SerializeFunc
    {
        /// <summary>
        /// 序列化前執行作業。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        /// <param name="value">物件。</param>
        private static void DoBeforeSerialize(SerializeFormat serializeFormat, object value)
        {
            // 序列化前的通知方法
            if (value is IObjectSerializeProcess serializeProcess) { serializeProcess.BeforeSerialize(serializeFormat); }
            // 標記開始序列化狀態
            if (value is IObjectSerialize objectSerialize) { objectSerialize.SetSerializeState(SerializeState.Serialize); }
        }

        /// <summary>
        /// 序列化後執行作業。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        /// <param name="value">物件。</param>
        private static void DoAfterSerialize(SerializeFormat serializeFormat, object value)
        {
            // 標記結束序列化
            if (value is IObjectSerialize objectSerialize) { objectSerialize.SetSerializeState(SerializeState.None); }
            // 序列化後的通知方法
            if (value is IObjectSerializeProcess objectSerializeProcess) { objectSerializeProcess.AfterSerialize(serializeFormat); }
        }

        /// <summary>
        /// 反序列化後執行作業。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        /// <param name="value">物件。</param>
        private static void DoAfterDeserialize(SerializeFormat serializeFormat, object value)
        {
            // 反序列化後的通知方法
            if (value is IObjectSerializeProcess objectSerializeProcess) { objectSerializeProcess.AfterDeserialize(serializeFormat); }
        }

        /// <summary>
        /// 將物件序列化為 XML 字串。
        /// </summary>
        /// <param name="value">物件。</param>
        public static string ObjectToXml(object value)
        {
            if (value == null)
                return string.Empty;

            // 序列化前執行作業
            DoBeforeSerialize(SerializeFormat.Xml, value);

            // 執行序列化寫入字串
            string xml = string.Empty;
            using (UTF8StringWriter writer = new UTF8StringWriter())
            {
                var serializer = XmlSerializerCache.Get(value.GetType());
                serializer.Serialize(writer, value);
                xml = writer.ToString();
            }

            // 序列化後執行作業
            DoAfterSerialize(SerializeFormat.Xml, value);
            return xml;
        }

        /// <summary>
        /// 將 XML 字串反序列化為物件。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="xml">XML 字串。</param>
        public static T XmlToObject<T>(string xml)
        {
            return (T)XmlToObject(xml, typeof(T));
        }

        /// <summary>
        /// 將 XML 字串反序列化為物件。
        /// </summary>
        /// <param name="xml">XML 字串。</param>
        /// <param name="type">物件型別。</param>
        public static object XmlToObject(string xml, Type type)
        {
            if (StrFunc.IsEmpty(xml))
                return default;

            object value;
            using (StringReader reader = new StringReader(xml))
            {
                var serializer = XmlSerializerCache.Get(type);
                value = serializer.Deserialize(reader);
            }

            // 反序列化後執行作業
            DoAfterDeserialize(SerializeFormat.Xml, value);
            return value;
        }

        /// <summary>
        /// 將物件序列化為 XML 檔案。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="filePath">XML 檔案路徑。</param>
        public static void ObjectToXmlFile(object value, string filePath)
        {
            string sXml;

            sXml = ObjectToXml(value);
            FileFunc.FileWriteText(filePath, sXml);
            // 設定序列化繫結檔案
            if (value is IObjectSerializeFile) { (value as IObjectSerializeFile).SetObjectFilePath(filePath); }
        }

        /// <summary>
        /// 將 XML 檔案反序列化為物件。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="filePath">XML 檔案路徑。</param>
        public static T XmlFileToObject<T>(string filePath)
        {
            try
            {
                // 讀取檔案內容，將 XML 字串反序列化為物件
                string xml = FileFunc.FileReadText(filePath);
                T value = XmlToObject<T>(xml);
                // 設定序列化繫結檔案
                if (value is IObjectSerializeFile objSerializeFile) { objSerializeFile.SetObjectFilePath(filePath); }
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"XmlFileToObject Error: {ex.Message}\nFileName: {filePath}", ex);
            }
        }

        /// <summary>
        /// 取得 JSON 序列化設定。
        /// </summary>
        /// <param name="ignoreDefaultValue">是否忽略預設值。</param>
        /// <param name="ignoreNullValue">是否忽略 Null 值。</param>
        /// <param name="includeTypeName">是否包含型別名稱。</param>
        private static JsonSerializerSettings GetJsonSerializerSettings(bool ignoreDefaultValue, bool ignoreNullValue, bool includeTypeName)
        {
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            // 忽略預設值
            if (ignoreDefaultValue)
                settings.DefaultValueHandling = DefaultValueHandling.Ignore;
            // 忽略 Null 值
            if (ignoreNullValue)
                settings.NullValueHandling = NullValueHandling.Ignore;
            // 加入型別名稱
            if (includeTypeName)
            {
                settings.TypeNameHandling = TypeNameHandling.Auto;
                settings.SerializationBinder = new JsonSerializationBinder();
            }
            // 列舉型別使用字串
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            return settings;
        }

        /// <summary>
        /// 將物件序列化為 JSON 字串。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="ignoreDefaultValue">是否忽略預設值。</param>
        /// <param name="ignoreNullValue">是否忽略 Null 值。</param>
        /// <param name="includeTypeName">是否包含型別名稱。</param>
        public static string ObjectToJson(object value, bool ignoreDefaultValue = true, bool ignoreNullValue = true, bool includeTypeName = true)
        {
            // 序列化前執行作業
            DoBeforeSerialize(SerializeFormat.Json, value);

            // 序列化為 JSON 字串
            var settings = GetJsonSerializerSettings(ignoreDefaultValue, ignoreNullValue, includeTypeName);
            string json = JsonConvert.SerializeObject(value, settings);

            // 序列化後執行作業
            DoAfterSerialize(SerializeFormat.Json, value);
            return json;
        }

        /// <summary>
        /// 將 JOSN 字串反序列化為物件。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="json">JSON 字串。</param>
        /// <param name="includeTypeName">是否包含型別名稱。</param>
        public static T JsonToObject<T>(string json, bool includeTypeName = true)
        {
            // 反序列化 JSON 字串
            var settings = GetJsonSerializerSettings(true, false, includeTypeName);
            object value = JsonConvert.DeserializeObject(json, typeof(T), settings);
            // 反序列化後執行作業
            DoAfterDeserialize(SerializeFormat.Json, value);
            return (T)value;
        }

        /// <summary>
        /// 將物件序列化為 JSON 檔案。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="filePath">JSON 檔案路徑。</param>
        public static void ObjectToJsonFile(object value, string filePath)
        {
            string json = ObjectToJson(value, true);
            FileFunc.FileWriteText(filePath, json);
            // 設定序列化繫結檔案
            if (value is IObjectSerializeFile objectSerializeFile) { objectSerializeFile.SetObjectFilePath(filePath); }
        }

        /// <summary>
        /// 將 JOSN 檔案反序列化為物件。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="filePath">JSON 檔案路徑。</param>
        public static T JsonFileToObject<T>(string filePath)
        {
            try
            {
                string json = FileFunc.FileReadText(filePath);
                T value = JsonToObject<T>(json);
                // 設定序列化繫結檔案
                if (value is IObjectSerializeFile objectSerializeFile) { objectSerializeFile.SetObjectFilePath(filePath); }
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"JsonFileToObject Error: {ex.Message}\nFileName: {filePath}", ex);
            }
        }

        /// <summary>
        /// 將物件序列化為二進位資料。
        /// </summary>
        /// <param name="value">物件。</param>
        public static byte[] ObjectToBinary(object value)
        {
            // 序列化前執行作業
            DoBeforeSerialize(SerializeFormat.Binary, value);

            byte[] bytes = null;
            using (MemoryStream stream = new MemoryStream())
            {
                var oFormatter = new BinaryFormatter();
                oFormatter.Serialize(stream, value);
                bytes = stream.ToArray();
            }

            // 序列化後執行作業
            DoAfterSerialize(SerializeFormat.Binary, value);
            return bytes;
        }

        /// <summary>
        /// 將二進位資料反序列化為物件。
        /// </summary>
        /// <param name="bytes">二進位資料。</param>
        public static object BinaryToObject(byte[] bytes)
        {
            object value;
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                formatter.Binder = new BinarySerializationBinder();
                value = formatter.Deserialize(stream);
            }

            // 反序列化後執行作業
            DoAfterDeserialize(SerializeFormat.Binary, value);
            return value;
        }

        /// <summary>
        /// 將二進位資料反序列化為物件。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="bytes">二進位資料。</param>
        public static T BinaryToObject<T>(byte[] bytes)
        {
            object value = BinaryToObject(bytes);
            return (T)value;
        }


    }
}
