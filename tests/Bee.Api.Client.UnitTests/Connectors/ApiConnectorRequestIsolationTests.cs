using System.ComponentModel;
using System.Data;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;

namespace Bee.Api.Client.UnitTests.Connectors
{
    /// <summary>
    /// 驗證 <see cref="Bee.Api.Client.Connectors.ApiConnector"/> 送出存檔請求時，伺服端拿到的
    /// <c>DataSet</c> 是副本：伺服端就地改寫它，不會改到呼叫端手上的那一份（ADR-032 D4）。
    /// </summary>
    /// <remarks>
    /// in-process 呼叫（<c>LocalApiProvider</c> + <c>Plain</c>）沒有序列化邊界，伺服端收到的就是
    /// Connector 交出去的物件。<c>FormBusinessObject.Save</c> 會改寫時間欄，寫入後 adapter 還會
    /// <c>AcceptChanges</c>；少了複製，畫面上的單據會跟著變成 UTC 值且失去未存狀態。
    /// 這裡以假的 provider 在「伺服端」就地改寫收到的物件，重現同一個形狀。
    /// </remarks>
    public class ApiConnectorRequestIsolationTests
    {
        private static readonly DateTime s_callerValue = new(2026, 1, 1, 17, 0, 0, DateTimeKind.Unspecified);
        private static readonly DateTime s_serverValue = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

        [Theory]
        [InlineData("")]
        [InlineData("Asia/Taipei")]
        [DisplayName("伺服端就地改寫存檔的 DataSet 時，呼叫端的 DataSet 應維持原值與未存狀態，且送出的值未經換算")]
        public async Task ExecuteAsync_ServerRewritesSavedDataSet_LeavesCallerDataSetUntouched(string userTimeZoneId)
        {
            var original = BuildEditedDataSet();
            var request = new SaveRequest { DataSet = original };
            DataSet? received = null;
            DateTime? receivedValue = null;

            await ApiConnectorTestHost.ExecuteAsUserAsync(request, userTimeZoneId, serverRequest =>
            {
                var save = Assert.IsType<SaveRequest>(serverRequest.Params!.Value);
                received = save.DataSet;
                var row = save.DataSet!.Tables[0].Rows[0];
                receivedValue = (DateTime)row["created_at"];

                row["created_at"] = s_serverValue;
                save.DataSet.AcceptChanges();
            });

            Assert.NotNull(received);
            Assert.NotSame(original, received);
            Assert.Equal(s_callerValue, receivedValue);

            Assert.Same(original, request.DataSet);
            var callerRow = original.Tables[0].Rows[0];
            Assert.Equal(DataRowState.Modified, callerRow.RowState);
            Assert.Equal(s_callerValue, (DateTime)callerRow["created_at"]);
        }

        private static DataSet BuildEditedDataSet()
        {
            var table = new DataTable("orders");
            table.AddColumn("created_at", FieldDbType.DateTime);
            table.AddColumn("remark", FieldDbType.String);
            table.Rows.Add(s_callerValue, "a");
            table.AcceptChanges();
            table.Rows[0]["remark"] = "edited";

            var dataSet = new DataSet("s");
            dataSet.Tables.Add(table);
            return dataSet;
        }
    }
}
