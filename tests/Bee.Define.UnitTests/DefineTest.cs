using Bee.Define.Collections;
using Bee.Define.Filters;
using Bee.Define.Forms;
using Bee.Define.Settings;
using System.Data;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Api.Contracts;
using Bee.Api.Contracts.System;

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
        [InlineData(DefineType.SystemSettings, typeof(SystemSettings))]
        [InlineData(DefineType.DatabaseSettings, typeof(DatabaseSettings))]
        [InlineData(DefineType.FormSchema, typeof(FormSchema))]
        public void GetDefineType_ValidType(DefineType defineType, Type expectedType)
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
        private void SerializeObject<T>(object value, bool isBinary = true, bool isXml = true, bool isJson = true)
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
        /// �إߴ��ըϥΪ���ƪ��C
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
            var items = new ListItemCollection();
            items.Add("01", "���ؤ@");
            items.Add("02", "���ؤG");
            items.Add("03", "���ؤT");
            SerializeObject<ListItemCollection>(items, true, true, true);
        }

        /// <summary>
        /// �Ѽƶ��X�ǦC�ơC
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
        /// �t�γ]�w�ǦC�ơC
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
        /// ���� Ping ��k�ǻ��Ѽƪ��ǦC�ơC
        /// </summary>
        [Fact(DisplayName = "Ping ��k�ǻ��Ѽƪ��ǦC��")]
        public void Ping_Serialize()
        {
            // �إ� TPingArgs �ë��w�ݩʻP�Ѽ�
            var args = new PingArgs
            {
                ClientName = "TestClient",
                TraceId = Guid.NewGuid().ToString()
            };
            // ���էǦC��
            SerializeObject<PingArgs>(args, true, false, true);

            // �إ� TPingResult �ë��w�ݩʻP�Ѽ�
            var result = new PingResult
            {
                Status = "pong",
                ServerTime = new DateTime(2025, 5, 16, 8, 30, 0, DateTimeKind.Utc),
                Version = "1.2.3",
                TraceId = Guid.NewGuid().ToString()
            };
            // ���էǦC��
            SerializeObject<PingResult>(result, true, false, true);
        }

        /// <summary>
        /// ���� Filters �i���T�ǦC�ƻP�٭��ݩʶ��X��ơC
        /// </summary>
        [Fact(DisplayName = "Filters �ǦC��")]
        public void Filters_Serialize()
        {
            var root = FilterGroup.All(
                FilterCondition.Equal("DeptId", 10),
                FilterGroup.Any(
                    FilterCondition.Contains("Name", "Lee"),
                    FilterCondition.Between("HireDate", new DateTime(2024, 1, 1), new DateTime(2024, 12, 31))
                )
            );
            // ���էǦC��
            SerializeObject<FilterGroup>(root, true, true, true);
        }
    }
}