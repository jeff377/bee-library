using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 驗證 <c>st_user.time_zone</c> 的讀取，含「未設值」的降級行為。
    /// </summary>
    /// <remarks>
    /// <see cref="UserRepository"/> 內部自行解析 <c>common</c> 分類，呼叫端無法指定資料庫，
    /// 因此這裡**不是** per-provider 矩陣——測試跑在 common 分類實際繫結的那一個 provider 上。
    /// <c>[DbFact]</c> 僅用於「該 provider 不可連線時自動跳過」。
    ///
    /// 空值不是例外情況而是預期狀態：欄位新增前既有的列沒有值，且採用自訂認證（不走
    /// <c>st_user</c>）的部署根本不會有對應的列。呼叫端據此決定 fallback。
    /// 設計背景見 docs/adr/adr-032-datetime-timezone.md（D12）。
    /// </remarks>
    public class UserRepositoryTimeZoneTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public UserRepositoryTimeZoneTests(SharedDbFixture fx) { _fx = fx; }

        private UserRepository CreateRepo()
            => new UserRepository(_fx.GetRequiredService<IDbConnectionManager>());

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetTimeZone('001') 應回傳 seed 使用者的時區")]
        public void GetTimeZone_SeedUser_ReturnsSeededZone()
        {
            Assert.Equal("Asia/Taipei", CreateRepo().GetTimeZone("001"));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetTimeZone 查無使用者應回傳空字串而非擲例外")]
        public void GetTimeZone_UnknownUser_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, CreateRepo().GetTimeZone("no-such-user"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("GetTimeZone 對空白 userId 應直接回傳空字串，不查資料庫")]
        public void GetTimeZone_BlankUserId_ReturnsEmpty(string userId)
        {
            Assert.Equal(string.Empty, CreateRepo().GetTimeZone(userId));
        }
    }
}
