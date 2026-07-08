using System.ComponentModel;
using System.Data;
using Bee.Api.Contracts;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.AuditLog;
using Bee.Definition.Logging;
using Bee.Definition.Paging;

namespace Bee.Api.Core.UnitTests.AuditLog
{
    /// <summary>
    /// AuditLog 軸 wire DTO 經 <see cref="MessagePackCodec"/> 的 round-trip 驗證：清單回應的
    /// <c>DataTable</c> + <c>PagingInfo</c>、明細回應的巢狀 <c>List&lt;RecordFieldChange&gt;</c>、
    /// 以及 <c>GetChangeLogRequest</c> 的 typed filter 欄位（含 nullable enum）皆能完整還原。
    /// </summary>
    public class AuditLogMessagePackTests
    {
        [Fact]
        [DisplayName("GetChangeLogRequest 應 round-trip 還原 typed filter（含 nullable enum / 分頁）")]
        public void GetChangeLogRequest_RoundTrip()
        {
            var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            var request = new GetChangeLogRequest
            {
                FromUtc = from,
                UserId = "demo",
                ProgId = "Employee",
                ChangeKind = ChangeKind.Delete,
                Paging = new PagingOptions { Page = 2, PageSize = 25, IncludeTotalCount = true },
            };

            var restored = MessagePackCodec.Deserialize<GetChangeLogRequest>(MessagePackCodec.Serialize(request));

            Assert.NotNull(restored);
            Assert.Equal(from, restored!.FromUtc);
            Assert.Null(restored.ToUtc);
            Assert.Equal("demo", restored.UserId);
            Assert.Equal("Employee", restored.ProgId);
            Assert.Equal(ChangeKind.Delete, restored.ChangeKind);
            Assert.Equal(2, restored.Paging!.Page);
            Assert.True(restored.Paging.IncludeTotalCount);
        }

        [Fact]
        [DisplayName("GetChangeLogResponse 帶 DataTable + PagingInfo 應 round-trip")]
        public void GetChangeLogResponse_RoundTrip_PreservesTableAndPaging()
        {
            var table = new DataTable("st_log_change");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("change_kind", typeof(int));
            var id = Guid.NewGuid();
            table.Rows.Add(id, (int)ChangeKind.Update);

            var response = new GetChangeLogResponse
            {
                Table = table,
                Paging = new PagingInfo { Page = 1, PageSize = 50, TotalCount = 1, HasMore = false },
            };

            var restored = MessagePackCodec.Deserialize<GetChangeLogResponse>(MessagePackCodec.Serialize(response));

            Assert.NotNull(restored);
            Assert.NotNull(restored!.Table);
            Assert.Single(restored.Table!.Rows);
            Assert.Equal(id, (Guid)restored.Table.Rows[0]["sys_rowid"]);
            Assert.Equal(1, restored.Paging!.TotalCount);
        }

        [Fact]
        [DisplayName("GetRecordHistoryResponse 帶 DataTable + PagingInfo 應 round-trip")]
        public void GetRecordHistoryResponse_RoundTrip()
        {
            var table = new DataTable("st_log_change");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Rows.Add(Guid.NewGuid());

            var response = new GetRecordHistoryResponse
            {
                ProgId = "Employee",
                RowKey = "R-1",
                Table = table,
                Paging = new PagingInfo { Page = 1, PageSize = 50, HasMore = false },
            };

            var restored = MessagePackCodec.Deserialize<GetRecordHistoryResponse>(MessagePackCodec.Serialize(response));

            Assert.NotNull(restored);
            Assert.Equal("Employee", restored!.ProgId);
            Assert.Equal("R-1", restored.RowKey);
            Assert.Single(restored.Table!.Rows);
        }

        [Fact]
        [DisplayName("GetChangeDetailResponse 帶巢狀 Fields 應完整 round-trip")]
        public void GetChangeDetailResponse_RoundTrip_PreservesFields()
        {
            var sysRowId = Guid.NewGuid();
            var response = new GetChangeDetailResponse
            {
                SysRowId = sysRowId,
                LogTime = new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc),
                UserId = "demo",
                ProgId = "Employee",
                RowKey = "R-1",
                ChangeKind = ChangeKind.Update,
                IsSensitive = true,
                Source = "Employee.Save",
                Fields =
                [
                    new RecordFieldChange { TableName = "st_employee", RowKey = "R-1", RowState = ChangeKind.Update, FieldName = "name", OldValue = "Alice", NewValue = "Alice Wang" },
                    new RecordFieldChange { TableName = "st_employee", RowKey = "R-1", RowState = ChangeKind.Update, FieldName = "note", OldValue = "keep", NewValue = null },
                ],
            };

            var restored = MessagePackCodec.Deserialize<GetChangeDetailResponse>(MessagePackCodec.Serialize(response));

            Assert.NotNull(restored);
            Assert.Equal(sysRowId, restored!.SysRowId);
            Assert.Equal(ChangeKind.Update, restored.ChangeKind);
            Assert.True(restored.IsSensitive);
            Assert.Equal(2, restored.Fields.Count);
            Assert.Equal("name", restored.Fields[0].FieldName);
            Assert.Equal("Alice Wang", restored.Fields[0].NewValue);
            Assert.Null(restored.Fields[1].NewValue);
        }

        [Fact]
        [DisplayName("GetChangeDetailResponse 空 Fields 應 round-trip 且不 NRE")]
        public void GetChangeDetailResponse_EmptyFields_RoundTrip()
        {
            var response = new GetChangeDetailResponse { SysRowId = Guid.NewGuid() };
            var restored = MessagePackCodec.Deserialize<GetChangeDetailResponse>(MessagePackCodec.Serialize(response));
            Assert.NotNull(restored);
            Assert.NotNull(restored!.Fields);
            Assert.Empty(restored.Fields);
        }
    }
}
