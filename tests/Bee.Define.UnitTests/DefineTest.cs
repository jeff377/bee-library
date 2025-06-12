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
        [InlineData(EDefineType.SystemSettings, typeof(TSystemSettings))]
        [InlineData(EDefineType.DatabaseSettings, typeof(TDatabaseSettings))]
        [InlineData(EDefineType.FormDefine, typeof(TFormDefine))]
        public void GetDefineType_ValidType(EDefineType defineType, Type expectedType)
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
            var items = new TListItemCollection();
            items.Add("01", "項目一");
            items.Add("02", "項目二");
            items.Add("03", "項目三");
            SerializeObject<TListItemCollection>(items, true, true, true);
        }

        /// <summary>
        /// 參數集合序列化。
        /// </summary>
        [Fact]
        public void SerializeParameters()
        {
            var parameters = new TParameterCollection
            {
                new TParameter("P1", 1),
                new TParameter("P2", "ABC"),
                new TParameter("P3", CreateDataTable()),
                new TParameter("P4", CreateDataSet())
            };
            SerializeObject<TParameterCollection>(parameters, true, false, true);
        }

        /// <summary>
        /// 系統設定序列化。
        /// </summary>
        [Fact]
        public void SystemSettings()
        {
            var settings = new TSystemSettings();
            settings.CommonConfiguration.Version = "1.0.0";
            settings.BackendConfiguration.DatabaseID = "default";
            SerializeObject<TSystemSettings>(settings, true, true, false);
        }

        /// <summary>
        /// 測試 Ping 方法傳遞參數的序列化。
        /// </summary>
        [Fact(DisplayName = "Ping 方法傳遞參數的序列化")]
        public void Ping_Serialize()
        {
            // 建立 TPingArgs 並指定屬性與參數
            var args = new TPingArgs
            {
                ClientName = "TestClient",
                TraceId = Guid.NewGuid().ToString()
            };
            // 測試序列化
            SerializeObject<TPingArgs>(args, true, false, true);

            // 建立 TPingResult 並指定屬性與參數
            var result = new TPingResult
            {
                Status = "pong",
                ServerTime = new DateTime(2025, 5, 16, 8, 30, 0, DateTimeKind.Utc),
                Version = "1.2.3",
                TraceId = Guid.NewGuid().ToString()
            };
            // 測試序列化
            SerializeObject<TPingResult>(result, true, false, true);
        }
    }
}