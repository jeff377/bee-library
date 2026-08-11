using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition.Filters;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// <see cref="PayloadZoneConverter"/> 測試：請求／回應兩個方向的載體換置，
    /// 以及請求方向必須還原呼叫端自己的物件。
    /// </summary>
    public class PayloadZoneConverterTests
    {
        private const string Taipei = "Asia/Taipei";
        private static readonly DateTime s_utc9Am = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

        private static DateTime ExpectedInTaipei(DateTime utcValue)
            => DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(utcValue, DateTimeKind.Unspecified),
                    TimeZoneInfo.FindSystemTimeZoneById(Taipei)),
                DateTimeKind.Unspecified);

        private static DataTable BuildTable()
        {
            var table = new DataTable("orders");
            table.AddColumn("created_at", FieldDbType.DateTime);
            table.Rows.Add(s_utc9Am);
            table.AcceptChanges();
            return table;
        }

        private static DataSet BuildDataSet()
        {
            var dataSet = new DataSet("s");
            dataSet.Tables.Add(BuildTable());
            return dataSet;
        }

        [Fact]
        [DisplayName("回應方向：GetListResponse.Table 轉為使用者時區")]
        public void ToUserZone_GetListResponse_ConvertsTable()
        {
            var response = new GetListResponse { Table = BuildTable() };

            PayloadZoneConverter.ToUserZone(response, Taipei);

            Assert.Equal(ExpectedInTaipei(s_utc9Am), (DateTime)response.Table!.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("回應方向：GetDataResponse.DataSet 轉為使用者時區")]
        public void ToUserZone_GetDataResponse_ConvertsDataSet()
        {
            var response = new GetDataResponse { DataSet = BuildDataSet() };

            PayloadZoneConverter.ToUserZone(response, Taipei);

            Assert.Equal(ExpectedInTaipei(s_utc9Am),
                (DateTime)response.DataSet!.Tables["orders"]!.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("請求方向：送出期間 DataSet 為 UTC 副本，還原後呼叫端物件不變")]
        public void ToUtc_SaveRequest_SwapsThenRestores()
        {
            var original = BuildDataSet();
            // 呼叫端手上的值以使用者時區呈現（Connector 收到回應時已轉過）。
            var userLocal = ExpectedInTaipei(s_utc9Am);
            original.Tables["orders"]!.Rows[0]["created_at"] = userLocal;
            original.AcceptChanges();
            var request = new SaveRequest { DataSet = original };

            using (PayloadZoneConverter.ToUtc(request, Taipei))
            {
                Assert.NotSame(original, request.DataSet);
                Assert.Equal(s_utc9Am, (DateTime)request.DataSet!.Tables["orders"]!.Rows[0]["created_at"]);
            }

            Assert.Same(original, request.DataSet);
            Assert.Equal(userLocal, (DateTime)original.Tables["orders"]!.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("請求方向：filter 的 DateTime 值轉為 UTC，DateOnly 不動，且原樹不被修改")]
        public void ToUtc_GetListRequest_ConvertsFilterWithoutMutatingSource()
        {
            var userLocal = ExpectedInTaipei(s_utc9Am);
            var day = new DateOnly(2026, 1, 1);
            var filter = FilterGroup.All(
                FilterCondition.Equal("created_at", userLocal),
                FilterCondition.Equal("order_date", day));
            var request = new GetListRequest { Filter = filter };

            using (PayloadZoneConverter.ToUtc(request, Taipei))
            {
                var converted = (FilterGroup)request.Filter!;
                Assert.Equal(s_utc9Am, ((FilterCondition)converted.Nodes[0]).Value);
                Assert.Equal(day, ((FilterCondition)converted.Nodes[1]).Value);
            }

            Assert.Same(filter, request.Filter);
            Assert.Equal(userLocal, ((FilterCondition)filter.Nodes[0]).Value);
        }

        [Fact]
        [DisplayName("空白時區為 no-op，不做任何換置")]
        public void BlankTimeZone_LeavesPayloadAlone()
        {
            var original = BuildDataSet();
            var request = new SaveRequest { DataSet = original };

            using (PayloadZoneConverter.ToUtc(request, string.Empty))
            {
                Assert.Same(original, request.DataSet);
            }

            var response = new GetListResponse { Table = BuildTable() };
            PayloadZoneConverter.ToUserZone(response, string.Empty);
            Assert.Equal(s_utc9Am, (DateTime)response.Table!.Rows[0]["created_at"]);
        }

        [Fact]
        [DisplayName("未涵蓋的型別與 null 一律略過")]
        public void UnknownPayload_IsIgnored()
        {
            Assert.Null(Record.Exception(() => PayloadZoneConverter.ToUserZone("plain", Taipei)));
            Assert.Null(Record.Exception(() => PayloadZoneConverter.ToUserZone(null, Taipei)));
            using (PayloadZoneConverter.ToUtc(null, Taipei)) { }
            using (PayloadZoneConverter.ToUtc("plain", Taipei)) { }
        }
    }
}
