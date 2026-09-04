using System.ComponentModel;
using Bee.Api.Contracts.AuditLog;
using Bee.Api.Core.MessagePack;
using Bee.Base.Serialization;
using Bee.Definition.Logging;

namespace Bee.Api.Core.UnitTests.MessagePack
{
    /// <summary>
    /// <c>Bee.Api.Contracts</c> 內具象 DTO 的 wire round-trip 測試（MessagePack + JSON）。
    /// </summary>
    /// <remarks>
    /// 契約組件多為介面，但夾雜少數具象 DTO，且都標了
    /// <c>[MessagePackObject(keyAsPropertyName: true)]</c>——代表它們會實際上 wire，先前卻只可能
    /// 被上層測試間接觸及，缺乏直接驗證。這些是純 wire DTO（不落磁碟），依序列化規範只需
    /// MessagePack + JSON 兩軸，不需 XML。
    /// </remarks>
    public class ContractsDtoRoundTripTests
    {
        [Fact]
        [DisplayName("RecordFieldChange MessagePack 與 JSON round-trip 應正確還原（含 null 值欄位）")]
        public void RecordFieldChange_RoundTrips_PreservesValues()
        {
            var original = new RecordFieldChange
            {
                TableName = "ft_order_detail",
                RowKey = "8f14e45f-ea8f-4b0c-9f2b-1a2b3c4d5e6f",
                RowState = ChangeKind.Update,
                FieldName = "qty",
                OldValue = "10",
                NewValue = null
            };

            AssertRecordFieldChange(MessagePackCodec.Deserialize<RecordFieldChange>(MessagePackCodec.Serialize(original)));
            AssertRecordFieldChange(JsonCodec.Deserialize<RecordFieldChange>(JsonCodec.Serialize(original)));
        }

        private static void AssertRecordFieldChange(RecordFieldChange? restored)
        {
            Assert.NotNull(restored);
            Assert.Equal("ft_order_detail", restored.TableName);
            Assert.Equal("8f14e45f-ea8f-4b0c-9f2b-1a2b3c4d5e6f", restored.RowKey);
            Assert.Equal(ChangeKind.Update, restored.RowState);
            Assert.Equal("qty", restored.FieldName);
            Assert.Equal("10", restored.OldValue);
            Assert.Null(restored.NewValue);
        }
    }
}
