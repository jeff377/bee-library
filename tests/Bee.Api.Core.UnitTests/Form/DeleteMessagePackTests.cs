using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.Form;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// <see cref="DeleteRequest"/> / <see cref="DeleteResponse"/> 的 MessagePack
    /// round-trip 驗證。最小型別,只需確認 RowId 與 RowsAffected 還原。
    /// </summary>
    public class DeleteMessagePackTests
    {
        [Fact]
        [DisplayName("DeleteRequest RowId 應 round-trip 還原")]
        public void DeleteRequest_RoundTrip_PreservesRowId()
        {
            var rowId = Guid.NewGuid();
            var request = new DeleteRequest { RowId = rowId };

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<DeleteRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(rowId, restored!.RowId);
        }

        [Fact]
        [DisplayName("DeleteResponse RowsAffected 應 round-trip 還原")]
        public void DeleteResponse_RoundTrip_PreservesRowsAffected()
        {
            var response = new DeleteResponse { RowsAffected = 5 };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<DeleteResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(5, restored!.RowsAffected);
        }

        [Fact]
        [DisplayName("DeleteResponse RowsAffected = 0 應 round-trip 為 0")]
        public void DeleteResponse_ZeroRowsAffected_RoundTrip()
        {
            var response = new DeleteResponse();

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<DeleteResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(0, restored!.RowsAffected);
        }
    }
}
