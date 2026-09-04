using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.System;
using Bee.Definition.Security;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 三個 <c>List&lt;T&gt;</c> wire 成員的**帶值** round-trip。
    /// </summary>
    /// <remarks>
    /// 這三個成員先前只被空集合走過。空集合的 round-trip 證不到 item formatter 有註冊、
    /// 也證不到每個欄位真的上得了 wire —— 集合寫成空陣列時，item 型別根本不會被解析。
    /// 因此每個測試都放**至少兩個** item（順序也一併釘住）並逐欄比對。
    /// </remarks>
    public class CollectionMemberRoundTripTests
    {
        [Fact]
        [DisplayName("ListApiKeysResponse.ApiKeys 應帶值 round-trip（含 nullable DateTime 的有值與 null 兩態）")]
        public void ListApiKeysResponse_ApiKeys_RoundTrips()
        {
            var issued = new DateTime(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc);
            var expired = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var original = new ListApiKeysResponse
            {
                ApiKeys =
                [
                    new ApiKeySummary
                    {
                        SysId = "key-1", SysName = "整合用", KeyType = ApiKeyType.ThirdParty,
                        Contact = "ops@example.invalid", Enabled = true,
                        IssuedAt = issued, ExpiredAt = expired,
                    },
                    // 兩個 nullable DateTime 皆為 null：null 與有值走的是不同的 wire 分支。
                    new ApiKeySummary { SysId = "key-2", Enabled = false },
                ],
            };

            var restored = MessagePackCodec.Deserialize<ListApiKeysResponse>(
                MessagePackCodec.Serialize(original));

            Assert.NotNull(restored);
            Assert.Equal(2, restored.ApiKeys.Count);

            var first = restored.ApiKeys[0];
            Assert.Equal("key-1", first.SysId);
            Assert.Equal("整合用", first.SysName);
            Assert.Equal(ApiKeyType.ThirdParty, first.KeyType);
            Assert.Equal("ops@example.invalid", first.Contact);
            Assert.True(first.Enabled);
            Assert.Equal(issued, first.IssuedAt);
            Assert.Equal(expired, first.ExpiredAt);

            var second = restored.ApiKeys[1];
            Assert.Equal("key-2", second.SysId);
            Assert.False(second.Enabled);
            Assert.Null(second.IssuedAt);
            Assert.Null(second.ExpiredAt);
        }
    }
}
