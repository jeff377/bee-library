using System.Data;
using Bee.Base;

namespace Bee.Define.UnitTests
{
    public class DefineTest
    {
        static DefineTest()
        {
            // .NET 8 �w�]���� BinaryFormatter�A�ݤ�ʱҥ�
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
        /// ����ǦC�ơC
        /// </summary>
        /// <param name="value">����C</param>
        /// <param name="isBinary">����G�i��ǦC�ơC</param>
        /// <param name="isXml">���� XML �ǦC�ơC</param>
        /// <param name="isJson">���� JSON �ǦC�ơC</param>
        private  void SerializeObject<T>(object value, bool isBinary = true, bool isXml = true, bool isJson = true)
        {
            object? value2;
            // �G�i��ǦC��
            if (isBinary)
            {
                byte[] bytes = SerializeFunc.ObjectToBinary(value);
                value2 = SerializeFunc.BinaryToObject<T>(bytes);
                Assert.NotNull(value2);
            }
            // XML �ǦC��
            if (isXml)
            {
                string xml = SerializeFunc.ObjectToXml(value);
                value2 = SerializeFunc.XmlToObject<T>(xml);
                Assert.NotNull(value2);
            }
            // JSON �ǦC��
            if (isJson)
            {
                string json = SerializeFunc.ObjectToJson(value);
                value2 = SerializeFunc.JsonToObject<T>(json);
                Assert.NotNull(value2);
            }
        }

        /// <summary>
        /// �إߴ��ո�ƶ��C
        /// </summary>
        private DataSet CreateDataSet()
        {
            var dataSet = new DataSet("TestDataSet");
            dataSet.Tables.Add(CreateDataTable());
            return dataSet;
        }

        /// <summary>
        /// �إߴ��ըϥΪ���ƪ�C
        /// </summary>
        private DataTable CreateDataTable()
        {
            var table = new DataTable("TestTable");
            table.Columns.Add("F1", typeof(int));
            table.Columns.Add("F2", typeof(string));
            table.Rows.Add(1, "�i�T");
            table.Rows.Add(2, "���|");
            return table;
        }

        /// <summary>
        /// �M�涵�ض��X�ǦC�ơC
        /// </summary>
        [Fact]
        public void SerializeListItems()
        {
            var items = new TListItemCollection();
            items.Add("01", "���ؤ@");
            items.Add("02", "���ؤG");
            items.Add("03", "���ؤT");
            SerializeObject<TListItemCollection>(items, true, true, true);
        }

        /// <summary>
        /// �Ѽƶ��X�ǦC�ơC
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
        /// �t�γ]�w�ǦC�ơC
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
        /// ���� Ping ��k�ǻ��Ѽƪ��ǦC�ơC
        /// </summary>
        [Fact(DisplayName = "Ping ��k�ǻ��Ѽƪ��ǦC��")]
        public void Ping_Serialize()
        {
            // �إ� TPingArgs �ë��w�ݩʻP�Ѽ�
            var args = new TPingArgs
            {
                ClientName = "TestClient",
                TraceId = Guid.NewGuid().ToString()
            };
            // ���էǦC��
            SerializeObject<TPingArgs>(args, true, false, true);

            // �إ� TPingResult �ë��w�ݩʻP�Ѽ�
            var result = new TPingResult
            {
                Status = "pong",
                ServerTime = new DateTime(2025, 5, 16, 8, 30, 0, DateTimeKind.Utc),
                Version = "1.2.3",
                TraceId = Guid.NewGuid().ToString()
            };
            // ���էǦC��
            SerializeObject<TPingResult>(result, true, false, true);
        }
    }
}