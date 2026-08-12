using System.ComponentModel;
using Bee.Api.Contracts.System;
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
        [DisplayName("CheckPackageUpdateRequest.Queries 應帶值 round-trip 並保留順序")]
        public void CheckPackageUpdateRequest_Queries_RoundTrips()
        {
            var original = new CheckPackageUpdateRequest
            {
                Queries =
                [
                    new PackageUpdateQuery
                    {
                        AppId = "Northwind", ComponentId = "Main",
                        CurrentVersion = "1.2.3", Platform = "Osx-arm64", Channel = "Beta",
                    },
                    new PackageUpdateQuery { AppId = "Tools", CurrentVersion = "0.9.0" },
                ],
            };

            var restored = MessagePackCodec.Deserialize<CheckPackageUpdateRequest>(
                MessagePackCodec.Serialize(original));

            Assert.NotNull(restored);
            Assert.Equal(2, restored.Queries.Count);

            var first = restored.Queries[0];
            Assert.Equal("Northwind", first.AppId);
            Assert.Equal("Main", first.ComponentId);
            Assert.Equal("1.2.3", first.CurrentVersion);
            Assert.Equal("Osx-arm64", first.Platform);
            Assert.Equal("Beta", first.Channel);

            // 第二個 item 的預設值同時證明「未賦值的欄位不會被前一個 item 汙染」。
            Assert.Equal("Tools", restored.Queries[1].AppId);
            Assert.Equal("0.9.0", restored.Queries[1].CurrentVersion);
        }

        [Fact]
        [DisplayName("CheckPackageUpdateResponse.Updates 應帶值 round-trip（含 enum 與 long 欄位）")]
        public void CheckPackageUpdateResponse_Updates_RoundTrips()
        {
            var original = new CheckPackageUpdateResponse
            {
                Updates =
                [
                    new PackageUpdateInfo
                    {
                        AppId = "Northwind", ComponentId = "Main", UpdateAvailable = true,
                        LatestVersion = "2.0.0", Mandatory = true, PackageSize = 123_456_789L,
                        Sha256 = "abc123", Delivery = PackageDelivery.Api,
                        PackageUrl = "https://example.invalid/pkg.zip", ReleaseNotes = "修了很多東西",
                    },
                    new PackageUpdateInfo { AppId = "Tools", UpdateAvailable = false },
                ],
            };

            var restored = MessagePackCodec.Deserialize<CheckPackageUpdateResponse>(
                MessagePackCodec.Serialize(original));

            Assert.NotNull(restored);
            Assert.Equal(2, restored.Updates.Count);

            var first = restored.Updates[0];
            Assert.True(first.UpdateAvailable);
            Assert.Equal("2.0.0", first.LatestVersion);
            Assert.True(first.Mandatory);
            Assert.Equal(123_456_789L, first.PackageSize);
            Assert.Equal("abc123", first.Sha256);
            Assert.Equal(PackageDelivery.Api, first.Delivery);
            Assert.Equal("https://example.invalid/pkg.zip", first.PackageUrl);
            Assert.Equal("修了很多東西", first.ReleaseNotes);

            Assert.False(restored.Updates[1].UpdateAvailable);
        }

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
