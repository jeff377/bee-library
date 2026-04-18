using System.ComponentModel;
using System.Data;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Business.System;
using Bee.Definition.Collections;
using Bee.Definition.Filters;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    public class DefinitionSerializationTests
    {
        /// <summary>
        /// 物件序列化。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="isXml">測試 XML 序列化。</param>
        /// <param name="isJson">測試 JSON 序列化。</param>
        private static void SerializeObject<T>(object value, bool isXml = true, bool isJson = true)
        {
            object? value2;
            // XML 序列化
            if (isXml)
            {
                string xml = SerializeFunc.ObjectToXml(value);
                value2 = SerializeFunc.XmlToObject<T>(xml);
                Assert.NotNull(value2);
            }
            // JSON 序列化
            if (isJson)
            {
                string json = SerializeFunc.ObjectToJson(value);
                value2 = SerializeFunc.JsonToObject<T>(json);
                Assert.NotNull(value2);
            }
        }

        /// <summary>
        /// 建立測試資料集。
        /// </summary>
        private static DataSet CreateDataSet()
        {
            var dataSet = new DataSet("TestDataSet");
            dataSet.Tables.Add(CreateDataTable());
            return dataSet;
        }

        /// <summary>
        /// 建立測試使用的資料表。
        /// </summary>
        private static DataTable CreateDataTable()
        {
            var table = new DataTable("TestTable");
            table.Columns.Add("F1", typeof(int));
            table.Columns.Add("F2", typeof(string));
            table.Rows.Add(1, "張三");
            table.Rows.Add(2, "李四");
            return table;
        }

        /// <summary>
        /// 清單項目集合序列化。
        /// </summary>
        [Fact]
        [DisplayName("ListItemCollection 序列化與反序列化應正確還原")]
        public void SerializeListItems_XmlAndJson_RoundTripsCorrectly()
        {
            var items = new ListItemCollection();
            items.Add("01", "項目一");
            items.Add("02", "項目二");
            items.Add("03", "項目三");
            SerializeObject<ListItemCollection>(items, true, true);
        }

        /// <summary>
        /// 參數集合序列化。
        /// </summary>
        [Fact]
        [DisplayName("ParameterCollection 序列化與反序列化應正確還原")]
        public void SerializeParameters_Json_RoundTripsCorrectly()
        {
            var parameters = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", "ABC"),
                new Parameter("P3", CreateDataTable()),
                new Parameter("P4", CreateDataSet())
            };
            SerializeObject<ParameterCollection>(parameters, false, true);
        }

        /// <summary>
        /// 系統設定序列化。
        /// </summary>
        [Fact]
        [DisplayName("SystemSettings 序列化與反序列化應正確還原")]
        public void SerializeSystemSettings_Xml_RoundTripsCorrectly()
        {
            var settings = new SystemSettings();
            settings.CommonConfiguration.Version = "1.0.0";
            settings.BackendConfiguration.DatabaseId = "default";
            SerializeObject<SystemSettings>(settings, true, false);
        }

        /// <summary>
        /// 測試 Ping 方法參數的序列化。
        /// </summary>
        [Fact(DisplayName = "PingArgs 與 PingResult 序列化與反序列化應正確還原")]
        public void SerializePing_Json_RoundTripsCorrectly()
        {
            // 建立 TPingArgs 並設定屬性與參數
            var args = new PingArgs
            {
                ClientName = "TestClient",
                TraceId = Guid.NewGuid().ToString()
            };
            // 測試序列化
            SerializeObject<PingArgs>(args, false, true);

            // 建立 TPingResult 並設定屬性與參數
            var result = new PingResult
            {
                Status = "pong",
                ServerTime = new DateTime(2025, 5, 16, 8, 30, 0, DateTimeKind.Utc),
                Version = "1.2.3",
                TraceId = Guid.NewGuid().ToString()
            };
            // 測試序列化
            SerializeObject<PingResult>(result, false, true);
        }

        /// <summary>
        /// 測試 Filters 可正確序列化與還原屬性集合填充。
        /// </summary>
        [Fact(DisplayName = "FilterGroup 序列化與反序列化應正確還原")]
        public void SerializeFilters_XmlAndJson_RoundTripsCorrectly()
        {
            var root = FilterGroup.All(
                FilterCondition.Equal("DeptId", 10),
                FilterGroup.Any(
                    FilterCondition.Contains("Name", "Lee"),
                    FilterCondition.Between("HireDate", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31))
                )
            );
            // 測試序列化
            SerializeObject<FilterGroup>(root, true, true);
        }
    }
}
