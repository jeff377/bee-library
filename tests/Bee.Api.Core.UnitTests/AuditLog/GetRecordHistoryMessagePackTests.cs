using System.ComponentModel;
using Bee.Api.Contracts;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.AuditLog;
using Bee.Definition.Logging;

namespace Bee.Api.Core.UnitTests.AuditLog
{
    /// <summary>
    /// GetRecordHistoryRequest / GetRecordHistoryResponse 經 <see cref="MessagePackCodec"/> 的 wire 層
    /// round-trip 驗證，重點：巢狀 <c>List&lt;RecordHistoryEntry&gt;</c> → <c>List&lt;RecordFieldChange&gt;</c>
    /// 的結構化 before/after 樹能完整還原，且空集合不 NRE。
    /// </summary>
    public class GetRecordHistoryMessagePackTests
    {
        [Fact]
        [DisplayName("GetRecordHistoryRequest 應 round-trip 還原 ProgId / RowKey")]
        public void GetRecordHistoryRequest_RoundTrip()
        {
            var request = new GetRecordHistoryRequest { ProgId = "Employee", RowKey = "R-100" };

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<GetRecordHistoryRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal("Employee", restored!.ProgId);
            Assert.Equal("R-100", restored.RowKey);
        }

        [Fact]
        [DisplayName("GetRecordHistoryResponse 帶巢狀 Changes/Fields 應完整 round-trip")]
        public void GetRecordHistoryResponse_RoundTrip_PreservesNestedChanges()
        {
            var sysRowId = Guid.NewGuid();
            var response = new GetRecordHistoryResponse
            {
                ProgId = "Employee",
                RowKey = "R-100",
                Changes =
                [
                    new RecordHistoryEntry
                    {
                        SysRowId = sysRowId,
                        LogTime = new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc),
                        UserId = "demo",
                        UserName = "Demo User",
                        ChangeKind = ChangeKind.Update,
                        IsSensitive = true,
                        Source = "Employee.Save",
                        Fields =
                        [
                            new RecordFieldChange
                            {
                                TableName = "st_employee",
                                RowKey = "R-100",
                                RowState = ChangeKind.Update,
                                FieldName = "name",
                                OldValue = "Alice",
                                NewValue = "Alice Wang",
                            },
                            new RecordFieldChange
                            {
                                TableName = "st_employee",
                                RowKey = "R-100",
                                RowState = ChangeKind.Update,
                                FieldName = "note",
                                OldValue = "keep",
                                NewValue = null,
                            },
                        ],
                    },
                ],
            };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetRecordHistoryResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Equal("Employee", restored!.ProgId);
            Assert.Equal("R-100", restored.RowKey);
            var entry = Assert.Single(restored.Changes);
            Assert.Equal(sysRowId, entry.SysRowId);
            Assert.Equal(ChangeKind.Update, entry.ChangeKind);
            Assert.True(entry.IsSensitive);
            Assert.Equal("Employee.Save", entry.Source);
            Assert.Equal(2, entry.Fields.Count);
            Assert.Equal("name", entry.Fields[0].FieldName);
            Assert.Equal("Alice", entry.Fields[0].OldValue);
            Assert.Equal("Alice Wang", entry.Fields[0].NewValue);
            Assert.Equal("note", entry.Fields[1].FieldName);
            Assert.Null(entry.Fields[1].NewValue);
        }

        [Fact]
        [DisplayName("GetRecordHistoryResponse 空 Changes 應 round-trip 且不 NRE")]
        public void GetRecordHistoryResponse_EmptyChanges_RoundTrip()
        {
            var response = new GetRecordHistoryResponse { ProgId = "Employee", RowKey = "R-1" };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetRecordHistoryResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.Changes);
            Assert.Empty(restored.Changes);
        }
    }
}
