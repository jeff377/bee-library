using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.Form;
using Bee.Definition.Paging;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// GetLookupRequest / GetLookupResponse 經 <see cref="MessagePackCodec"/> 的 wire 層
    /// round-trip 序列化驗證。
    /// </summary>
    public class GetLookupMessagePackTests
    {
        [Fact]
        [DisplayName("GetLookupRequest 帶 SearchText 與 Paging 應 round-trip 還原")]
        public void GetLookupRequest_RoundTrip_PreservesValues()
        {
            var request = new GetLookupRequest
            {
                SearchText = "台積",
                Paging = new PagingOptions { Page = 2, PageSize = 30, IncludeTotalCount = true },
            };

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<GetLookupRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal("台積", restored!.SearchText);
            Assert.NotNull(restored.Paging);
            Assert.Equal(2, restored.Paging!.Page);
            Assert.Equal(30, restored.Paging.PageSize);
            Assert.True(restored.Paging.IncludeTotalCount);
        }

        [Fact]
        [DisplayName("GetLookupRequest 預設值應 round-trip 為相等內容")]
        public void GetLookupRequest_DefaultValues_RoundTrip()
        {
            var request = new GetLookupRequest();

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<GetLookupRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(string.Empty, restored!.SearchText);
            Assert.Null(restored.Paging);
        }

        [Fact]
        [DisplayName("GetLookupResponse 帶 DataTable 與 PagingInfo 應 round-trip 還原")]
        public void GetLookupResponse_RoundTrip_PreservesTableAndPaging()
        {
            var table = new DataTable("Customer");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add(Guid.NewGuid(), "C001", "客戶甲");
            var response = new GetLookupResponse
            {
                Table = table,
                Paging = new PagingInfo { Page = 1, PageSize = 100, TotalCount = 1, HasMore = false },
            };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetLookupResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.Table);
            Assert.Equal(3, restored.Table!.Columns.Count);
            Assert.Single(restored.Table.Rows);
            Assert.Equal("C001", restored.Table.Rows[0]["sys_id"]);
            Assert.Equal("客戶甲", restored.Table.Rows[0]["sys_name"]);
            Assert.NotNull(restored.Paging);
            Assert.Equal(1, restored.Paging!.TotalCount);
        }

        [Fact]
        [DisplayName("GetLookupResponse.Table = null 應 round-trip 為 null")]
        public void GetLookupResponse_NullTable_RoundTrip()
        {
            var response = new GetLookupResponse { Table = null };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetLookupResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Null(restored!.Table);
            Assert.Null(restored.Paging);
        }
    }
}
