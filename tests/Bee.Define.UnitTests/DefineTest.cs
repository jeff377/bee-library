using System.Data;
using Bee.Base;

namespace Bee.Define.UnitTests
{
    public class DefineTest
    {
        static DefineTest()
        {
            // .NET 8 預設停用 BinaryFormatter，需手動啟用
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        [Theory]
        [InlineData(DefineType.SystemSettings, typeof(SystemSettings))]
        [InlineData(DefineType.DatabaseSettings, typeof(DatabaseSettings))]
        [InlineData(DefineType.FormDefine, typeof(FormDefine))]
        public void GetDefineType_ValidType(DefineType defineType, Type expectedType)
        {
            // Act
            var result = DefineFunc.GetDefineType(defineType);

            // Assert
            Assert.Equal(expectedType, result);
        }

        /// <summary>
        /// 物件序列化。
        /// </summary>
        /// <param name="value">物件。</param>
        /// <param name="isBinary">執行二進位序列化。</param>
        /// <param name="isXml">執行 XML 序列化。</param>
        /// <param name="isJson">執行 JSON 序列化。</param>
        private  void SerializeObject<T>(object value, bool isBinary = true, bool isXml = true, bool isJson = true)
        {
            object? value2;
            // 二進位序列化
            if (isBinary)
            {
                byte[] bytes = SerializeFunc.ObjectToBinary(value);
                value2 = SerializeFunc.BinaryToObject<T>(bytes);
                Assert.NotNull(value2);
            }
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
        private DataSet CreateDataSet()
        {
            var dataSet = new DataSet("TestDataSet");
            dataSet.Tables.Add(CreateDataTable());
            return dataSet;
        }

        /// <summary>
        /// 建立測試使用的資料表。
        /// </summary>
        private DataTable CreateDataTable()
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
        public void SerializeListItems()
        {
            var items = new ListItemCollection();
            items.Add("01", "項目一");
            items.Add("02", "項目二");
            items.Add("03", "項目三");
            SerializeObject<ListItemCollection>(items, true, true, true);
        }

        /// <summary>
        /// 參數集合序列化。
        /// </summary>
        [Fact]
        public void SerializeParameters()
        {
            var parameters = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", "ABC"),
                new Parameter("P3", CreateDataTable()),
                new Parameter("P4", CreateDataSet())
            };
            SerializeObject<ParameterCollection>(parameters, true, false, true);
        }

        /// <summary>
        /// 系統設定序列化。
        /// </summary>
        [Fact]
        public void SystemSettings()
        {
            var settings = new SystemSettings();
            settings.CommonConfiguration.Version = "1.0.0";
            settings.BackendConfiguration.DatabaseId = "default";
            SerializeObject<SystemSettings>(settings, true, true, false);
        }

        /// <summary>
        /// 測試 Ping 方法傳遞參數的序列化。
        /// </summary>
        [Fact(DisplayName = "Ping 方法傳遞參數的序列化")]
        public void Ping_Serialize()
        {
            // 建立 TPingArgs 並指定屬性與參數
            var args = new PingArgs
            {
                ClientName = "TestClient",
                TraceId = Guid.NewGuid().ToString()
            };
            // 測試序列化
            SerializeObject<PingArgs>(args, true, false, true);

            // 建立 TPingResult 並指定屬性與參數
            var result = new PingResult
            {
                Status = "pong",
                ServerTime = new DateTime(2025, 5, 16, 8, 30, 0, DateTimeKind.Utc),
                Version = "1.2.3",
                TraceId = Guid.NewGuid().ToString()
            };
            // 測試序列化
            SerializeObject<PingResult>(result, true, false, true);
        }
    }
}